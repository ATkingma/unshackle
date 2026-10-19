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

    private static readonly string[] s_taskFactories = ["FromResult", "FromException", "FromCanceled"];

    private static readonly string[] s_moqChain = ["ReturnsAsync", "ThrowsAsync", "Throws", "Callback", "Verifiable"];

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
        var namespaces = new SortedSet<string>(StringComparer.Ordinal) { "System.Threading" };
        foreach ((ISymbol holder, List<GenericNameSyntax> declared) in holders)
        {
            Rewrite(type, model, holder, declared, edits, problems, namespaces);
        }

        if (problems.Count > 0)
        {
            return PatternResult.Manual($"RequestHandlerDelegate is used in a way that is not migrated: {string.Join("; ", problems)}. Rewrite it by hand: Mediator's MessageHandlerDelegate<TMessage, TResponse> takes the message and the token and returns a ValueTask.");
        }

        TypeDeclarationSyntax result = type.ReplaceNodes(edits.Keys, (original, current) => edits[original](current));
        return PatternResult.Fixed(result, [.. namespaces]);
    }

    private static void Rewrite(
        TypeDeclarationSyntax type,
        SemanticModel model,
        ISymbol holder,
        List<GenericNameSyntax> declared,
        Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>> edits,
        List<string> problems,
        SortedSet<string> namespaces)
    {
        if (holder is not (ILocalSymbol or IFieldSymbol { DeclaredAccessibility: Accessibility.Private }))
        {
            problems.Add($"{holder.Name} is not a private field or local, so its uses cannot all be found");
            return;
        }

        var requestTypes = new List<ITypeSymbol>();
        var handedArguments = new List<ExpressionSyntax>();
        var invocations = new List<InvocationExpressionSyntax>();
        var returns = new List<InvocationExpressionSyntax>();
        var moqInvocations = new List<InvocationExpressionSyntax>();
        var moqReturns = new List<ArgumentSyntax>();
        bool moq = IsMoqMock(holder is ILocalSymbol local ? local.Type : ((IFieldSymbol)holder).Type);
        foreach (IdentifierNameSyntax use in type.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, holder)))
        {
            ExpressionSyntax expression = use.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } member && member.Name == use ? member : use;
            if (moq)
            {
                MoqUse(holder, expression, model, requestTypes, handedArguments, moqInvocations, moqReturns, problems);
                continue;
            }

            switch (expression.Parent)
            {
                case AssignmentExpressionSyntax assignment when assignment.Left == expression && IsSubstituteFor(assignment.Right):
                    break;
                case ArgumentSyntax { NameColon: null, Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } list } argument
                    when RequestOf(call, list.Arguments.IndexOf(argument), model) is { } handled:
                    requestTypes.Add(handled);
                    handedArguments.Add(expression);
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

        int distinctRequests = requestTypes.Select(t => t.ToDisplayString()).Distinct(StringComparer.Ordinal).Count();
        if (distinctRequests == 0)
        {
            problems.Add($"{holder.Name} is not handed to a behavior's Handle, so Mediator's request type cannot be derived");
            return;
        }

        // MessageHandlerDelegate<TMessage, TResponse> is invariant in TMessage, so one field for several request types is typed on IMessage
        // and every Handle call gets an adapter lambda that converts the delegate to that behavior's own request type.
        bool shared = distinctRequests > 1;
        TypeSyntax request = shared
            ? SyntaxFactory.IdentifierName("IMessage")
            : SyntaxFactory.ParseTypeName(requestTypes[0].ToMinimalDisplayString(model, declared[0].SpanStart));
        if (shared)
        {
            string message = FreeName(type, "message");
            string token = FreeName(type, "cancellationToken");
            foreach (ExpressionSyntax handed in handedArguments)
            {
                edits[handed] = current => SyntaxFactory.ParseExpression($"({message}, {token}) => {current.WithoutTrivia()}({message}, {token})").WithTriviaFrom(current);
            }
        }

        // The behavior's Handle now returns a ValueTask: a call that is not awaited on the spot (FluentActions.Invoking(() => sut.Handle(..)),
        // Func<Task> act = () => sut.Handle(..)) gets .AsTask(), so it keeps the Task type it had.
        foreach (InvocationExpressionSyntax call in handedArguments.Select(a => a.Parent?.Parent?.Parent).OfType<InvocationExpressionSyntax>().Distinct())
        {
            if (call.Parent is not AwaitExpressionSyntax && !edits.ContainsKey(call))
            {
                edits[call] = current => SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, ((ExpressionSyntax)current).WithoutTrailingTrivia(), SyntaxFactory.IdentifierName("AsTask")))
                    .WithTrailingTrivia(current.GetTrailingTrivia());
            }
        }

        foreach (GenericNameSyntax reference in declared)
        {
            if (reference.Parent is QualifiedNameSyntax)
            {
                problems.Add($"{reference.ToString()} at line {Line(reference)} is namespace-qualified");
                continue;
            }

            // A real lambda gets Mediator's (message, cancellationToken) parameters and a ValueTask; a method group cannot be renamed into the new signature.
            if (reference.Parent is VariableDeclarationSyntax { Variables: [{ Initializer.Value: { } initializer }] } && !IsSubstituteFor(initializer))
            {
                if (initializer is not LambdaExpressionSyntax lambda)
                {
                    problems.Add($"{holder.Name} at line {Line(reference)} is initialized with a delegate that is not a lambda or a Substitute.For mock");
                    continue;
                }

                RealLambda(holder, lambda, reference.TypeArgumentList.Arguments[0], model, edits, problems, namespaces);
            }

            edits[reference] = current => SyntaxFactory.GenericName(SyntaxFactory.Identifier("MessageHandlerDelegate"))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(MediatRSyntax.CommaList([request, ((GenericNameSyntax)current).TypeArgumentList.Arguments[0]])))
                .WithTriviaFrom(current);
        }

        if (invocations.Count > 0)
        {
            namespaces.Add("NSubstitute");
        }

        foreach (InvocationExpressionSyntax invoke in moqInvocations)
        {
            string cancellation = invoke.ArgumentList.Arguments.Count == 1 ? invoke.ArgumentList.Arguments[0].ToString() : "It.IsAny<CancellationToken>()";
            edits[invoke.ArgumentList] = current => SyntaxFactory.ParseArgumentList($"(It.IsAny<{request}>(), {cancellation})").WithTriviaFrom(current);
        }

        foreach (ArgumentSyntax argument in moqReturns)
        {
            ReturnsArgument(argument, model, edits, problems);
            namespaces.Add("System.Threading.Tasks");
        }

        foreach (InvocationExpressionSyntax invoke in invocations)
        {
            // MediatR 12.5's delegate takes an optional CancellationToken: a given one stays the second argument.
            string cancellation = invoke.ArgumentList.Arguments.Count == 1 ? invoke.ArgumentList.Arguments[0].ToString() : "Arg.Any<CancellationToken>()";
            edits[invoke.ArgumentList] = current => SyntaxFactory.ParseArgumentList($"(Arg.Any<{request}>(), {cancellation})").WithTriviaFrom(current);
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

    // A real lambda: _ => Task.FromResult(x) → (_, _) => ValueTask.FromResult(x). MediatR 12.5's delegate has one (optional CancellationToken)
    // parameter, Mediator's has the message and the token; a non-async body's returned Task becomes a ValueTask.
    private static void RealLambda(
        ISymbol holder,
        LambdaExpressionSyntax lambda,
        TypeSyntax response,
        SemanticModel model,
        Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>> edits,
        List<string> problems,
        SortedSet<string> namespaces)
    {
        ParameterSyntax? parameter = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [var only] } => only,
            _ => null,
        };
        if (lambda is ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: > 1 } || parameter is { Type: not null })
        {
            problems.Add($"the lambda of {holder.Name} at line {Line(lambda)} has typed or extra parameters");
            return;
        }

        string token = parameter?.Identifier.ValueText ?? "_";
        IParameterSymbol? tokenSymbol = parameter is null ? null : model.GetDeclaredSymbol(parameter);
        if (token == "_" && tokenSymbol is not null && lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Any(n => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, tokenSymbol)))
        {
            problems.Add($"the lambda of {holder.Name} at line {Line(lambda)} uses its parameter _, which becomes a discard");
            return;
        }

        if (lambda.AsyncKeyword.IsKind(SyntaxKind.None))
        {
            List<ExpressionSyntax?> returned = lambda.ExpressionBody is { } body
                ? [body]
                : [.. lambda.Block!.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                    .OfType<ReturnStatementSyntax>().Select(r => r.Expression)];
            foreach (ExpressionSyntax? value in returned)
            {
                ReturnedTask(holder, lambda, value, response, model, edits, problems);
            }
        }

        namespaces.Add("System.Threading.Tasks");
        edits[lambda] = current =>
        {
            var old = (LambdaExpressionSyntax)current;
            SyntaxTriviaList after = old switch
            {
                SimpleLambdaExpressionSyntax simple => simple.Parameter.GetTrailingTrivia(),
                ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.GetTrailingTrivia(),
                _ => SyntaxFactory.TriviaList(SyntaxFactory.Space),
            };
            return SyntaxFactory.ParenthesizedLambdaExpression()
                .WithModifiers(old.Modifiers)
                .WithParameterList(SyntaxFactory.ParseParameterList($"(_, {token})").WithTrailingTrivia(after))
                .WithArrowToken(old.ArrowToken)
                .WithBlock(old.Block)
                .WithExpressionBody(old.ExpressionBody)
                .WithTriviaFrom(old);
        };
    }

    // Task.FromResult/FromException/FromCanceled → ValueTask.X; another Task<T> → new ValueTask<TRes>(task); a throw stays.
    private static void ReturnedTask(
        ISymbol holder,
        LambdaExpressionSyntax lambda,
        ExpressionSyntax? value,
        TypeSyntax response,
        SemanticModel model,
        Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode>> edits,
        List<string> problems)
    {
        if (value is ThrowExpressionSyntax)
        {
            return;
        }

        if (value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax taskMember } call
            && s_taskFactories.Any(name => MediatRSyntax.IsTaskMember(call, model, name)))
        {
            edits[taskMember] = current => MediatRSyntax.OnValueTask((MemberAccessExpressionSyntax)current);
            return;
        }

        if (value is not null && model.GetTypeInfo(value).Type is INamedTypeSymbol { Name: "Task", Arity: 1 } task
            && task.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks")
        {
            edits[value] = current => SyntaxFactory.ParseExpression($"new ValueTask<{response.WithoutTrivia()}>({current.WithoutTrivia()})").WithTriviaFrom(current);
            return;
        }

        problems.Add($"the lambda of {holder.Name} at line {Line((SyntaxNode?)value ?? lambda)} returns something other than a Task<T>");
    }

    // A Moq Mock<RequestHandlerDelegate<TRes>>: Setup/Verify(x => x.Invoke()) get Mediator's two arguments, .Object is the handed delegate.
    private static void MoqUse(
        ISymbol holder,
        ExpressionSyntax expression,
        SemanticModel model,
        List<ITypeSymbol> requestTypes,
        List<ExpressionSyntax> handedArguments,
        List<InvocationExpressionSyntax> invocations,
        List<ArgumentSyntax> returns,
        List<string> problems)
    {
        if (expression.Parent is AssignmentExpressionSyntax assignment && assignment.Left == expression
            && assignment.Right is BaseObjectCreationExpressionSyntax)
        {
            return;
        }

        if (expression.Parent is not MemberAccessExpressionSyntax member || member.Expression != expression)
        {
            problems.Add($"{holder.Name} at line {Line(expression)} is used other than through Setup, Verify or Object");
            return;
        }

        string name = member.Name.Identifier.ValueText;
        if (name == "Object" && member.Parent is ArgumentSyntax { NameColon: null, Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } list } argument
            && RequestOf(call, list.Arguments.IndexOf(argument), model) is { } handled)
        {
            requestTypes.Add(handled);
            handedArguments.Add(member);
            return;
        }

        if (member.Parent is not InvocationExpressionSyntax configure)
        {
            problems.Add($"{holder.Name}.{name} at line {Line(expression)} is not migrated");
            return;
        }

        if (name is "Reset" or "VerifyAll" or "VerifyNoOtherCalls" || (name == "Verify" && configure.ArgumentList.Arguments.Count == 0))
        {
            return;
        }

        if (name is not ("Setup" or "Verify")
            || configure.ArgumentList.Arguments.FirstOrDefault()?.Expression is not SimpleLambdaExpressionSyntax { ExpressionBody: InvocationExpressionSyntax invoke } setup
            || !InvokesParameter(invoke, setup.Parameter.Identifier.ValueText))
        {
            problems.Add($"{holder.Name}.{name} at line {Line(expression)} is not a Setup or Verify of x => x.Invoke()");
            return;
        }

        invocations.Add(invoke);
        if (name == "Verify")
        {
            return;
        }

        // The chain after Setup: ReturnsAsync/ThrowsAsync also configure a ValueTask<T>; Returns(Task.FromResult(x)) → Returns(ValueTask.FromResult(x)).
        for (SyntaxNode link = configure;
            link.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: string next, Parent: InvocationExpressionSyntax nextCall } access && access.Expression == link;
            link = nextCall)
        {
            if (next == "Returns" && nextCall.ArgumentList.Arguments is [var value])
            {
                returns.Add(value);
            }
            else if (!s_moqChain.Contains(next))
            {
                problems.Add($"{holder.Name}.Setup(...).{next} at line {Line(nextCall)} is not migrated");
            }
        }
    }

    private static bool InvokesParameter(InvocationExpressionSyntax invoke, string parameter) =>
        invoke.ArgumentList.Arguments.Count <= 1 && invoke.Expression switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoke", Expression: IdentifierNameSyntax receiver } => receiver.Identifier.ValueText == parameter,
            IdentifierNameSyntax receiver => receiver.Identifier.ValueText == parameter,
            _ => false,
        };

    private static bool IsMoqMock(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "Mock", Arity: 1 } mock && mock.ContainingNamespace.ToDisplayString() == "Moq";

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
            case TypeArgumentListSyntax { Parent: GenericNameSyntax { Identifier.ValueText: "Mock" } mock }:
                return mock.Parent switch
                {
                    VariableDeclarationSyntax { Variables.Count: 1 } declaration when declaration.Type == mock => model.GetDeclaredSymbol(declaration.Variables[0]),
                    ObjectCreationExpressionSyntax { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } } => model.GetDeclaredSymbol(declarator),
                    ObjectCreationExpressionSyntax { Parent: AssignmentExpressionSyntax assignment } creation when assignment.Right == creation => model.GetSymbolInfo(assignment.Left).Symbol,
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

    // A lambda parameter name that no identifier in the type already uses, so it cannot shadow or clash.
    private static string FreeName(SyntaxNode scope, string name)
    {
        HashSet<string> used = [.. scope.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText)];
        while (used.Contains(name))
        {
            name += "_";
        }

        return name;
    }

    private static SyntaxNode? InnermostType(SyntaxNode node) => node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}
