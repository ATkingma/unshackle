using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>
/// Test code that mocks the <c>next</c> of a pipeline behavior: <c>RequestHandlerDelegate&lt;TRes&gt;</c> → <c>MessageHandlerDelegate&lt;TReq, TRes&gt;</c>,
/// <c>next.Invoke().Returns(x)</c> → <c>next.Invoke(Arg.Any&lt;TReq&gt;(), Arg.Any&lt;CancellationToken&gt;()).Returns(x)</c>.
/// </summary>
public class RequestHandlerDelegatePattern : IPattern
{
    private static readonly string[] s_configurations = ["Returns", "ReturnsForAnyArgs"];

    private static readonly string[] s_receivedChecks = ["Received", "DidNotReceive"];

    public string Id => "mediatr.request-handler-delegate";

    public string Description =>
        "RequestHandlerDelegate<TRes> mock or field handed to a behavior's Handle → MessageHandlerDelegate<TReq, TRes>, Invoke() → Invoke(Arg.Any<TReq>(), Arg.Any<CancellationToken>()); anything else → Manual with the line";

    // The type, not the reference: all uses of one delegate (declaration, mock, Invoke, Handle call) are rewritten together.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        DelegateReferences(root, model).Select(InnermostType).OfType<TypeDeclarationSyntax>().Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var type = (TypeDeclarationSyntax)node;
        List<GenericNameSyntax> references = [.. DelegateReferences(type, model).Where(r => InnermostType(r) == type)];
        var problems = new List<string>();
        var holders = new Dictionary<ISymbol, List<GenericNameSyntax>>(SymbolEqualityComparer.Default);
        foreach (GenericNameSyntax reference in references)
        {
            if (Holder(reference, model) is { } holder)
            {
                if (!holders.TryGetValue(holder, out List<GenericNameSyntax>? list))
                {
                    holders[holder] = list = [];
                }

                list.Add(reference);
            }
            else
            {
                problems.Add($"{reference.ToString()} at line {Line(reference)} is not a field or local that is mocked or handed to a behavior");
            }
        }

        var edits = new Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>>();
        foreach ((ISymbol holder, List<GenericNameSyntax> declared) in holders)
        {
            Rewrite(type, model, holder, declared, edits, problems);
        }

        if (problems.Count > 0)
        {
            return PatternResult.Manual($"RequestHandlerDelegate is used in a way that is not migrated: {string.Join("; ", problems)}. Rewrite it by hand: Mediator's MessageHandlerDelegate<TMessage, TResponse> takes the message and the token and returns a ValueTask.");
        }

        TypeDeclarationSyntax result = type.ReplaceNodes(edits.Keys, (original, current) => edits[original](current));
        return PatternResult.Fixed(result, "System.Threading", "NSubstitute");
    }

    private static void Rewrite(
        TypeDeclarationSyntax type,
        SemanticModel model,
        ISymbol holder,
        List<GenericNameSyntax> declared,
        Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>> edits,
        List<string> problems)
    {
        if (holder is not (ILocalSymbol or IFieldSymbol { DeclaredAccessibility: Accessibility.Private }))
        {
            problems.Add($"{holder.Name} is not a private field or local, so its uses cannot all be found");
            return;
        }

        var requestTypes = new List<ITypeSymbol>();
        var invocations = new List<InvocationExpressionSyntax>();
        var returns = new List<InvocationExpressionSyntax>();
        foreach (IdentifierNameSyntax use in type.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, holder)))
        {
            ExpressionSyntax expression = use.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } member && member.Name == use ? member : use;
            switch (expression.Parent)
            {
                case AssignmentExpressionSyntax assignment when assignment.Left == expression && IsSubstituteFor(assignment.Right):
                    break;
                case ArgumentSyntax { NameColon: null, Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } list } argument
                    when RequestOf(call, list.Arguments.IndexOf(argument), model) is { } handled:
                    requestTypes.Add(handled);
                    break;
                default:
                    if (ConfiguredInvocation(expression) is { } invoke)
                    {
                        invocations.Add(invoke);
                        returns.AddRange(ReturnsCalls(invoke));
                    }
                    else
                    {
                        problems.Add($"{holder.Name} at line {Line(use)} is used other than as a mock configuration or as the next argument of a behavior's Handle");
                    }

                    break;
            }
        }

        if (requestTypes.Select(t => t.ToDisplayString()).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            problems.Add($"{holder.Name} is not handed to exactly one request type of a behavior's Handle, so Mediator's request type cannot be derived");
            return;
        }

        TypeSyntax request = SyntaxFactory.ParseTypeName(requestTypes[0].ToMinimalDisplayString(model, declared[0].SpanStart));
        foreach (GenericNameSyntax reference in declared)
        {
            if (reference.Parent is QualifiedNameSyntax)
            {
                problems.Add($"{reference.ToString()} at line {Line(reference)} is namespace-qualified");
                continue;
            }

            // A real delegate (lambda, method group) cannot become the new signature by renaming the type.
            if (reference.Parent is VariableDeclarationSyntax { Variables: [{ Initializer.Value: { } initializer }] } && !IsSubstituteFor(initializer))
            {
                problems.Add($"{holder.Name} at line {Line(reference)} is initialized with a real delegate, not a Substitute.For mock");
                continue;
            }

            edits[reference] = current => SyntaxFactory.GenericName(SyntaxFactory.Identifier("MessageHandlerDelegate"))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(MediatRSyntax.CommaList([request, ((GenericNameSyntax)current).TypeArgumentList.Arguments[0]])))
                .WithTriviaFrom(current);
        }

        foreach (InvocationExpressionSyntax invoke in invocations)
        {
            // MediatR 12.5's delegate takes an optional CancellationToken: a given one stays the second argument.
            string token = invoke.ArgumentList.Arguments.Count == 1 ? invoke.ArgumentList.Arguments[0].ToString() : "Arg.Any<CancellationToken>()";
            edits[invoke.ArgumentList] = current => SyntaxFactory.ParseArgumentList($"(Arg.Any<{request}>(), {token})").WithTriviaFrom(current);
        }

        foreach (ArgumentSyntax argument in returns.SelectMany(r => r.ArgumentList.Arguments))
        {
            ReturnsArgument(argument, model, edits, problems);
        }
    }

    // NSubstitute's Returns(T) also configures a ValueTask<T>; a Task.FromResult(x) becomes ValueTask.FromResult(x), other Tasks and lambdas are by hand.
    private static void ReturnsArgument(
        ArgumentSyntax argument, SemanticModel model, Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>> edits, List<string> problems)
    {
        ExpressionSyntax value = argument.Expression;
        if (value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax taskMember } fromResult
            && MediatRSyntax.IsTaskMember(fromResult, model, "FromResult"))
        {
            edits[taskMember] = current => MediatRSyntax.OnValueTask((MemberAccessExpressionSyntax)current);
            return;
        }

        ITypeSymbol? valueType = value is AnonymousFunctionExpressionSyntax ? null : model.GetTypeInfo(value).Type;
        if (valueType is null || valueType.Name is "Task" or "ValueTask" or "Func")
        {
            problems.Add($"the Returns argument at line {Line(value)} is a Task or a callback, not the response; Invoke now returns a ValueTask");
        }
    }

    // holder.Invoke(), holder() and holder.Received().Invoke(), as the receiver of Returns or in a Received check: the only places Arg.Any is valid.
    private static InvocationExpressionSyntax? ConfiguredInvocation(ExpressionSyntax holder)
    {
        ExpressionSyntax receiver = holder;
        if (holder.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: string check, Parent: InvocationExpressionSyntax checkCall } checkAccess
            && checkAccess.Expression == holder && s_receivedChecks.Contains(check))
        {
            receiver = checkCall;
        }

        InvocationExpressionSyntax? invoke = receiver.Parent switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoke", Parent: InvocationExpressionSyntax call } access when access.Expression == receiver => call,
            InvocationExpressionSyntax call when call.Expression == receiver => call,
            _ => null,
        };
        if (invoke is null || invoke.ArgumentList.Arguments.Count > 1)
        {
            return null;
        }

        bool checkedCall = receiver != holder;
        return checkedCall || ReturnsCalls(invoke).Any() ? invoke : null;
    }

    private static IEnumerable<InvocationExpressionSyntax> ReturnsCalls(InvocationExpressionSyntax invoke) =>
        invoke.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: string name, Parent: InvocationExpressionSyntax call } access
            && access.Expression == invoke && s_configurations.Contains(name)
            ? [call]
            : [];

    // The request type of a behavior's Handle that takes the delegate as its next argument.
    private static ITypeSymbol? RequestOf(InvocationExpressionSyntax call, int index, SemanticModel model) =>
        model.GetSymbolInfo(call).Symbol is IMethodSymbol { Name: "Handle", Parameters.Length: 3 } method
            && index == 1
            && method.ContainingType.AllInterfaces.Prepend(method.ContainingType).Any(i => MediatRSyntax.IsMediatR(i, "IPipelineBehavior"))
            ? method.Parameters[0].Type
            : null;

    private static bool IsSubstituteFor(ExpressionSyntax expression) =>
        expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "For" } } };

    // The field or local a delegate type belongs to: its declared type, or the Substitute.For<...>() assigned to it.
    private static ISymbol? Holder(GenericNameSyntax reference, SemanticModel model)
    {
        switch (reference.Parent)
        {
            case VariableDeclarationSyntax { Variables.Count: 1 } declaration when declaration.Type == reference:
                return model.GetDeclaredSymbol(declaration.Variables[0]);
            case TypeArgumentListSyntax { Parent: GenericNameSyntax { Identifier.ValueText: "For" } forName }
                when forName.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax substitute }:
                return substitute.Parent switch
                {
                    EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } => model.GetDeclaredSymbol(declarator),
                    AssignmentExpressionSyntax assignment when assignment.Right == substitute => model.GetSymbolInfo(assignment.Left).Symbol,
                    _ => null,
                };
            default:
                return null;
        }
    }

    // Everything except the delegate of a behavior's own Handle: PipelineBehaviorPattern rewrites that one.
    private static IEnumerable<GenericNameSyntax> DelegateReferences(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodesAndSelf().OfType<GenericNameSyntax>()
            .Where(g => g.Identifier.ValueText == "RequestHandlerDelegate"
                && model.GetSymbolInfo(g).Symbol is INamedTypeSymbol { Arity: 1 } symbol && MediatRSyntax.IsMediatR(symbol, "RequestHandlerDelegate")
                && !IsBehaviorParameter(g, model));

    private static bool IsBehaviorParameter(GenericNameSyntax reference, SemanticModel model) =>
        reference.Parent is ParameterSyntax { Parent.Parent: MethodDeclarationSyntax method }
        && model.GetDeclaredSymbol(method) is IMethodSymbol { Name: "Handle", ContainingType: { } owner }
        && owner.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "IPipelineBehavior"));

    private static SyntaxNode? InnermostType(SyntaxNode node) => node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}
