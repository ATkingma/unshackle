using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// The member options of one CreateMap beyond Ignore and MapFrom that the profile rewrite understands (TAAK-255):
/// ForMember(d => d.X, o => o.Condition(s => cond)) or o.PreCondition, alone or in a block with o.MapFrom(s => expr), and
/// ForPath(d => d.A.B, o => o.MapFrom(s => expr)). A ForPath of one member is a MapFrom (measured: null-safe the same way).
/// </summary>
public class MemberOptions
{
    private const string AutoMapperAssembly = "AutoMapper";

    /// <summary>Destination member → the lambda of its Condition or PreCondition.</summary>
    public Dictionary<string, SimpleLambdaExpressionSyntax?> Conditions { get; } = new(StringComparer.Ordinal);

    /// <summary>Destination member A → the members B of ForPath(d => d.A.B, ...) with their MapFrom lambdas, in source order.</summary>
    public Dictionary<string, List<(string Member, SimpleLambdaExpressionSyntax MapFrom)>> Paths { get; } = new(StringComparer.Ordinal);

    /// <summary>Why a recognized member option is not migrated (a precise Manual reason), or null.</summary>
    public string? Unsupported { get; set; }

    /// <summary>Whether any ForPath was read, also one of a single member (which went into the MapFrom options).</summary>
    public bool HasForPath { get; private set; }

    public bool IsEmpty => Conditions.Count == 0 && Paths.Count == 0;

    /// <summary>All lambdas, keyed by a description of the member they belong to (for the checks that a lambda can move).</summary>
    public Dictionary<string, SimpleLambdaExpressionSyntax?> Lambdas()
    {
        Dictionary<string, SimpleLambdaExpressionSyntax?> lambdas = new(StringComparer.Ordinal);
        foreach ((string member, SimpleLambdaExpressionSyntax? condition) in Conditions)
        {
            lambdas["Condition of " + member] = condition;
        }

        foreach ((string member, List<(string Member, SimpleLambdaExpressionSyntax MapFrom)> path) in Paths)
        {
            foreach ((string inner, SimpleLambdaExpressionSyntax mapFrom) in path)
            {
                lambdas[member + "." + inner] = mapFrom;
            }
        }

        return lambdas;
    }

    /// <summary>
    /// Reads a ForMember with Condition/PreCondition or a ForPath into options (a MapFrom) and this; false for any other form
    /// (with <see cref="Unsupported"/> set when the form is known but not migrated).
    /// </summary>
    public bool TryParse(InvocationExpressionSyntax call, SemanticModel model, Dictionary<string, SimpleLambdaExpressionSyntax?> options)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);
        if (call is not { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var kind } }
            || call.ArgumentList.Arguments is not [{ Expression: SimpleLambdaExpressionSyntax target }, { Expression: SimpleLambdaExpressionSyntax option }]
            || Path(target) is not { Count: > 0 } path)
        {
            return false;
        }

        List<InvocationExpressionSyntax> calls = option.Body switch
        {
            InvocationExpressionSyntax single => [single],
            BlockSyntax block when block.Statements.All(s => s is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax }) =>
                [.. block.Statements.Select(s => (InvocationExpressionSyntax)((ExpressionStatementSyntax)s).Expression)],
            _ => [],
        };
        SimpleLambdaExpressionSyntax? condition = null;
        SimpleLambdaExpressionSyntax? mapFrom = null;
        string member = string.Join('.', path);
        foreach (InvocationExpressionSyntax optionCall in calls)
        {
            if (optionCall.Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver, Name: IdentifierNameSyntax name }
                || receiver.Identifier.ValueText != option.Parameter.Identifier.ValueText
                || model.GetSymbolInfo(optionCall).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly })
            {
                return false;
            }

            SimpleLambdaExpressionSyntax? lambda = optionCall.ArgumentList.Arguments is [{ Expression: SimpleLambdaExpressionSyntax { Body: ExpressionSyntax } l }] ? l : null;
            switch (name.Identifier.ValueText)
            {
                case "MapFrom" when lambda is not null && mapFrom is null:
                    mapFrom = lambda;
                    break;
                case "Condition" or "PreCondition" when kind == "ForMember" && lambda is not null && condition is null:
                    condition = lambda;
                    break;
                case "Condition" or "PreCondition":
                    Unsupported = $"{name.Identifier.ValueText} for {member} is not a lambda of the source alone (it reads the destination or the member values)";
                    return false;
                case "NullSubstitute" or "UseDestinationValue" or "ConvertUsing" or "AllowNull" or "DoNotAllowNull" or "SetMappingOrder" or "ExplicitExpansion":
                    Unsupported = $"{name.Identifier.ValueText} for {member} is not migrated";
                    return false;
                default:
                    return false;
            }
        }

        if (kind == "ForPath")
        {
            HasForPath = true;
            if (mapFrom is null || condition is not null || path.Count > 2)
            {
                Unsupported = path.Count > 2 ? $"ForPath to {member} goes more than one object deep" : null;
                return false;
            }

            if (path.Count == 1)
            {
                return options.TryAdd(path[0], mapFrom);
            }

            if (!Paths.TryGetValue(path[0], out List<(string Member, SimpleLambdaExpressionSyntax MapFrom)>? members))
            {
                Paths[path[0]] = members = [];
            }

            if (members.Any(m => m.Member == path[1]))
            {
                return false;
            }

            members.Add((path[1], mapFrom));
            return true;
        }

        return kind == "ForMember" && path.Count == 1 && condition is not null
            && (mapFrom is null || options.TryAdd(path[0], mapFrom))
            && Conditions.TryAdd(path[0], condition);
    }

    // The members of `d => d.A.B` (outermost first), or null when the body is not such a path from the parameter.
    private static List<string>? Path(SimpleLambdaExpressionSyntax lambda)
    {
        List<string> path = [];
        var current = lambda.Body as ExpressionSyntax;
        while (current is MemberAccessExpressionSyntax or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            if (current is PostfixUnaryExpressionSyntax suppressed)
            {
                current = suppressed.Operand;
                continue;
            }

            var member = (MemberAccessExpressionSyntax)current;
            path.Insert(0, member.Name.Identifier.ValueText);
            current = member.Expression;
        }

        return current is IdentifierNameSyntax name && name.Identifier.ValueText == lambda.Parameter.Identifier.ValueText ? path : null;
    }
}
