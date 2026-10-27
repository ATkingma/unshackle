using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Unshackle.Core.Fixes.EfBulk.Patterns;

/// <summary>
/// <c>q.Update(x => new T { A = e })</c> / <c>UpdateFromQuery</c> (+Async) of Z.EntityFramework.Plus/Extensions →
/// <c>q.ExecuteUpdate(s => s.SetProperty(x => x.A, x => e))</c>. Only plain values (literals, captured variables, the entity's own
/// scalar properties and operators on them); anything EF Core may translate differently is Manual (TAAK-342 council question 4).
/// </summary>
public class UpdatePattern : IPattern
{
    private static readonly Dictionary<string, string> s_names = new(StringComparer.Ordinal)
    {
        ["Update"] = "ExecuteUpdate",
        ["UpdateFromQuery"] = "ExecuteUpdate",
        ["UpdateAsync"] = "ExecuteUpdateAsync",
        ["UpdateFromQueryAsync"] = "ExecuteUpdateAsync",
    };

    private const string SettersName = "setters";

    public string Id => "efbulk.update";

    public string Description => "q.Update(x => new T { A = e }) (Z.EntityFramework) → q.ExecuteUpdate(s => s.SetProperty(x => x.A, x => e))";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => EfBulkCall.OldMethod(i, model, s_names) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var invocation = (InvocationExpressionSyntax)node;
        SimpleNameSyntax name = EfBulkCall.Name(invocation)!;
        string target = s_names[name.Identifier.ValueText];
        if (!EfBulkCall.HasNative(model, "ExecuteUpdate"))
        {
            return PatternResult.Manual(EfBulkReason.NoNativeExecute, "ExecuteUpdate needs EF Core 7+ with a relational provider.");
        }

        bool isAsync = target.EndsWith("Async", StringComparison.Ordinal);
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        bool restMaps = arguments.Count == 1 || (arguments.Count == 2 && isAsync && EfBulkCall.IsCancellationToken(arguments[1], model));
        if (arguments.Count == 0 || !restMaps || arguments[0].NameColon is not null)
        {
            return PatternResult.Manual(EfBulkReason.BatchOptions, "a batch builder (BatchSize, Executing, ...) has no ExecuteUpdate equivalent.");
        }

        if (Factory(arguments[0].Expression, model) is not ({ } parameter, { } initializer))
        {
            return PatternResult.Manual(EfBulkReason.UpdateFactoryShape,
                "the update factory is not an inline lambda x => new T { ... } with property assignments only.");
        }

        var setters = new List<(string Property, ExpressionSyntax Value)>();
        foreach (ExpressionSyntax expression in initializer.Expressions)
        {
            if (expression is not AssignmentExpressionSyntax { Left: IdentifierNameSyntax left, Right: { } value } assignment
                || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                || model.GetSymbolInfo(left).Symbol is not IPropertySymbol property
                || !IsScalar(property.Type))
            {
                return PatternResult.Manual(EfBulkReason.UpdateValueNotProvable,
                    $"'{expression}' does not set a scalar property; ExecuteUpdate cannot set navigations or owned types.");
            }

            if (!IsPlainValue(value, model, parameter))
            {
                return PatternResult.Manual(EfBulkReason.UpdateValueNotProvable,
                    $"'{value}' may translate to SQL differently in EF Core (method calls, conditionals, server-side members).");
            }

            setters.Add((left.Identifier.ValueText, value.WithoutTrivia()));
        }

        if (setters.Count == 0)
        {
            return PatternResult.Manual(EfBulkReason.UpdateFactoryShape, "the initializer sets no property; ExecuteUpdate needs at least one setter.");
        }

        string settersName = FreeName(model, invocation.SpanStart, parameter.Name);
        ExpressionSyntax chain = IdentifierName(settersName);
        foreach ((string property, ExpressionSyntax value) in setters)
        {
            chain = InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, chain, IdentifierName("SetProperty")),
                ArgumentList(SeparatedList(
                [
                    Argument(SimpleLambdaExpression(Parameter(Identifier(parameter.Name)),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(parameter.Name), IdentifierName(property)))),
                    Argument(SimpleLambdaExpression(Parameter(Identifier(parameter.Name)), value)),
                ])));
        }

        ArgumentSyntax lambda = Argument(SimpleLambdaExpression(Parameter(Identifier(settersName)), chain)).NormalizeWhitespace();
        InvocationExpressionSyntax renamed = EfBulkCall.Renamed(invocation, name, target);
        InvocationExpressionSyntax rewritten = renamed.WithArgumentList(
            renamed.ArgumentList.WithArguments(renamed.ArgumentList.Arguments.Replace(renamed.ArgumentList.Arguments[0], lambda)));
        return PatternResult.Fixed(rewritten, EfBulkCall.EfCoreNamespace);
    }

    // x => new T { ... } with T the lambda parameter's own type, no constructor arguments and an object initializer.
    private static (IParameterSymbol? Parameter, InitializerExpressionSyntax? Initializer) Factory(ExpressionSyntax expression, SemanticModel model)
    {
        (ParameterSyntax? parameter, CSharpSyntaxNode? body) = expression switch
        {
            SimpleLambdaExpressionSyntax simple => (simple.Parameter, simple.Body),
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [var single] } parenthesized => (single, parenthesized.Body),
            _ => (null, null),
        };
        InitializerExpressionSyntax? initializer = body switch
        {
            ObjectCreationExpressionSyntax { Initializer: { } i } creation when creation.ArgumentList is null or { Arguments.Count: 0 } => i,
            ImplicitObjectCreationExpressionSyntax { Initializer: { } i } creation when creation.ArgumentList.Arguments.Count == 0 => i,
            _ => null,
        };
        if (parameter is null || initializer is null || !initializer.IsKind(SyntaxKind.ObjectInitializerExpression)
            || model.GetDeclaredSymbol(parameter) is not { } symbol || model.GetTypeInfo(body!).Type is not { } created
            || !SymbolEqualityComparer.Default.Equals(created, symbol.Type))
        {
            return (null, null);
        }

        return (symbol, initializer);
    }

    // Values EF Core translates like a parameter or a column: literals, captured locals/fields/properties (evaluated on the client in both),
    // the entity's own scalar properties, and operators, casts and parentheses on those.
    private static bool IsPlainValue(ExpressionSyntax value, SemanticModel model, IParameterSymbol parameter) => value switch
    {
        LiteralExpressionSyntax or DefaultExpressionSyntax => true,
        ParenthesizedExpressionSyntax p => IsPlainValue(p.Expression, model, parameter),
        CastExpressionSyntax c => IsPlainValue(c.Expression, model, parameter),
        PostfixUnaryExpressionSyntax u when u.IsKind(SyntaxKind.SuppressNullableWarningExpression) => IsPlainValue(u.Operand, model, parameter),
        PrefixUnaryExpressionSyntax u when u.IsKind(SyntaxKind.UnaryMinusExpression) || u.IsKind(SyntaxKind.LogicalNotExpression) =>
            IsPlainValue(u.Operand, model, parameter),
        BinaryExpressionSyntax b => model.GetSymbolInfo(b).Symbol is not IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator }
            && IsPlainValue(b.Left, model, parameter) && IsPlainValue(b.Right, model, parameter),
        IdentifierNameSyntax or MemberAccessExpressionSyntax => IsPlainName(value, model, parameter),
        _ => false,
    };

    private static bool IsPlainName(ExpressionSyntax value, SemanticModel model, IParameterSymbol parameter)
    {
        ISymbol? symbol = model.GetSymbolInfo(value).Symbol;
        if (value is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver }
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, parameter))
        {
            // x.P: a column of the entity itself; a navigation (x.Customer.Name) would need a join.
            return symbol is IPropertySymbol property && IsScalar(property.Type);
        }

        return symbol switch
        {
            // The lambda parameter itself is the entity, not a value.
            IParameterSymbol p => !SymbolEqualityComparer.Default.Equals(p, parameter),
            ILocalSymbol => true,
            // Fields (also static readonly ones and enum members) are evaluated on the client.
            IFieldSymbol => IsReceiverPlain(value, model, parameter),
            // Instance properties of captured values are evaluated on the client; static ones (DateTime.Now) EF may run on the server.
            IPropertySymbol property => !property.IsStatic && IsReceiverPlain(value, model, parameter),
            _ => false,
        };
    }

    private static bool IsReceiverPlain(ExpressionSyntax value, SemanticModel model, IParameterSymbol parameter) => value switch
    {
        MemberAccessExpressionSyntax access => model.GetSymbolInfo(access.Expression).Symbol is INamedTypeSymbol
            || IsPlainName(access.Expression, model, parameter) || access.Expression is ThisExpressionSyntax,
        _ => true,
    };

    private static bool IsScalar(ITypeSymbol type)
    {
        ITypeSymbol underlying = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var inner] } ? inner : type;
        return underlying.SpecialType is not SpecialType.None and not SpecialType.System_Object
            || underlying.TypeKind == TypeKind.Enum
            || underlying.ToDisplayString() is "System.DateTimeOffset" or "System.TimeSpan" or "System.Guid" or "System.DateOnly" or "System.TimeOnly"
            || underlying is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte };
    }

    // "setters", or "setters2", ... when the name is taken at the call (a lambda parameter must not shadow a local in older C#).
    private static string FreeName(SemanticModel model, int position, string entityParameter)
    {
        string name = SettersName;
        for (int n = 2; name == entityParameter || !model.LookupSymbols(position, name: name).IsEmpty; n++)
        {
            name = SettersName + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return name;
    }
}
