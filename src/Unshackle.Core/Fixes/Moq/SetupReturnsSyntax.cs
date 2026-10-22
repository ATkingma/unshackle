using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// `m.Setup(x => x.M(..)).Returns(v)[.Verifiable()];` → `m.Object.M(..).Returns(v);` (moq-queue.md row 3); ReturnsAsync(v) becomes
// Returns(Task.FromResult<R>(v)) or Returns(new ValueTask<R>(v)). `m.Setup(x => x.Void(..))[.Verifiable()];` configures nothing a
// loose mock does not already do, so it goes (row 10: what it verified moves to the Verify()/VerifyAll expansion). The .Object goes
// when moq.create converts the mock.
public static class SetupReturnsSyntax
{
    public const string TasksNamespace = "System.Threading.Tasks";

    public const string ExceptionsNamespace = "NSubstitute.ExceptionExtensions";

    public const string ConfigureNamespace = "NSubstitute.Extensions";

    public static bool IsSetup(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Setup" or "SetupGet" }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "Setup" or "SetupGet" } method && MoqSyntax.IsMockOfT(method.ContainingType);

    // SetupGet (council round 12, M7: reported as moq.properties).
    public static bool IsSetupGet(InvocationExpressionSyntax setup) =>
        setup.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "SetupGet" };

    // The statement a Setup is part of, when it has a shape this fix rewrites: Setup, then optionally Returns/ReturnsAsync, then optionally Verifiable().
    public static ExpressionStatementSyntax? Statement(InvocationExpressionSyntax setup) => Chain(setup).Statement;

    public static bool IsVerifiable(InvocationExpressionSyntax setup) => Chain(setup).Verifiable;

    // Which pattern rewrites the Setup: Throws/ThrowsAsync (moq.throws, row 6), Returns with a function (moq.returns-lambda, row 7), else moq.setup-returns.
    public static SetupKind Kind(InvocationExpressionSyntax setup) => Chain(setup) is { Callback: not null } ? SetupKind.Callback : Chain(setup).Returns switch
    {
        { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Throws" or "ThrowsAsync" } } => SetupKind.Throws,
        { ArgumentList.Arguments: [{ Expression: AnonymousFunctionExpressionSyntax }] } => SetupKind.ReturnsLambda,
        _ => SetupKind.Returns,
    };

    // The rewritten statement (in Node; Removed when the statement goes), or why the Setup cannot be rewritten; Type is set only when
    // the rewrite needs System.Threading.Tasks.
    public static MockCall Plan(InvocationExpressionSyntax setup, SemanticModel model)
    {
        SetupChain chain = Chain(setup);
        MockCall plan = PlanValue(setup, chain.Statement, chain.Returns, chain.Verifiable, chain.Callback is not null, model);
        return chain.Callback is null || plan.Blocker is not null ? plan : WithCallback(setup, chain, plan, model);
    }

    // Row 8: Callback(f) runs f with the call's arguments. With a value it becomes `.AndDoes(ci => ..)` after the Returns; with a Returns function
    // (NSubstitute runs AndDoes first), with Throws, or with another setup of the member (AndDoes/Do runs for every matching call) it is
    // Manual. Without a value it becomes
    // `m.Object.When(x => x.M(..)).Do(ci => ..)`, which leaves the answer as it was (Moq's default, pinned by moq.create).
    private static MockCall WithCallback(InvocationExpressionSyntax setup, SetupChain chain, MockCall plan, SemanticModel model)
    {
        InvocationExpressionSyntax callback = chain.Callback!;
        if (chain.Returns?.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Throws" or "ThrowsAsync" })
        {
            return MockCall.Blocked(MoqReason.CallbackOrder, "Callback with Throws: NSubstitute's AndDoes never runs after a throw, Moq's Callback runs first");
        }

        if (chain.Returns?.ArgumentList.Arguments is [{ Expression: AnonymousFunctionExpressionSyntax }])
        {
            return MockCall.Blocked(MoqReason.CallbackOrder, "Callback with a Returns function: NSubstitute runs AndDoes before the function, Moq after it (proven at run time)");
        }

        if (HasOtherSetup(setup, model))
        {
            return MockCall.Blocked(MoqReason.SetupsOverlap,
                "Callback on a member with another setup: NSubstitute runs an AndDoes/Do for every matching call, also the ones a later setup answers; Moq only the matched setup's callback");
        }

        var lambda = (SimpleLambdaExpressionSyntax)setup.ArgumentList.Arguments[0].Expression;
        if (plan.Member is not IMethodSymbol method || callback.ArgumentList.Arguments is not [{ Expression: LambdaExpressionSyntax function }])
        {
            return MockCall.Blocked(MoqReason.CallbackShape, "Callback on a property, or with a method group or several arguments");
        }

        MockCall converted = CallLambda.Convert(function, method.Parameters, model);
        if (converted.Blocker is not null)
        {
            return MockCall.Blocked(MoqReason.CallbackShape, converted.Blocker.Reason);
        }

        if (plan.Node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax configured } valueStatement)
        {
            ExpressionSyntax andDoes = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, configured, SyntaxFactory.IdentifierName("AndDoes")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument((ExpressionSyntax)converted.Node!))));
            return new()
            {
                Node = valueStatement.WithExpression(andDoes),
                Member = plan.Member,
                ForAnyArguments = plan.ForAnyArguments,
                Type = plan.Type,
                Verifiable = plan.Verifiable,
                Configured = plan.Configured,
            };
        }

        string parameter = lambda.Parameter.Identifier.Text;
        var when = MockCall.Rewrite(lambda, SyntaxFactory.IdentifierName(parameter), model);
        if (when.Blocker is not null)
        {
            return when;
        }

        var receiver = (MemberAccessExpressionSyntax)setup.Expression;
        StatementSyntax rewritten = SyntaxFactory.ParseStatement($"{receiver.Expression.WithoutTrivia()}.Object.When({parameter} => {when.Node}).Do({converted.Node});");

        // Not set up for the answer: the member keeps its pin.
        return new() { Node = rewritten.WithTriviaFrom(chain.Statement!), Member = plan.Member, Verifiable = plan.Verifiable };
    }

    private static MockCall PlanValue(
        InvocationExpressionSyntax setup, ExpressionStatementSyntax? statement, InvocationExpressionSyntax? returns, bool verifiable, bool hasCallback, SemanticModel model)
    {
        if (statement is null)
        {
            return MockCall.Blocked(MoqReason.SetupShape, "only `m.Setup(..).Returns(v);` or `.ReturnsAsync(v);` as a statement; Callback, Throws, Verifiable and sequences are later moq-queue.md rows");
        }

        if (setup.ArgumentList.Arguments is not [var lambda] || setup.Expression is not MemberAccessExpressionSyntax { Expression: var receiver })
        {
            return MockCall.Blocked(MoqReason.SetupShape, "a Setup without a single lambda argument");
        }

        ExpressionSyntax objectAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression, receiver.WithoutTrivia(), SyntaxFactory.IdentifierName("Object"));

        // A configuration call in NSubstitute is a call: it runs a matching earlier configuration (its Returns function, AndDoes, Do or
        // Throws), which a Moq Setup never does. Configure() turns that off; it is written when the member has another setup in scope.
        bool configure = HasOtherSetup(setup, model);
        if (configure)
        {
            objectAccess = SyntaxFactory.ParseExpression($"{objectAccess}.Configure()");
        }

        var call = MockCall.Rewrite(lambda.Expression, objectAccess, model);
        if (call.Blocker is not null)
        {
            return call;
        }

        if (returns is null && hasCallback)
        {
            return new() { Member = call.Member, Verifiable = verifiable };
        }

        MockCall plan = PlanAfterCall(setup, statement, returns, verifiable, call, lambda, model);
        return configure && plan.Node is not null && plan.Blocker is null && !plan.Removed
            ? new()
            {
                Node = plan.Node, Member = plan.Member, ForAnyArguments = plan.ForAnyArguments, Type = plan.Type, Verifiable = plan.Verifiable,
                Throws = plan.Throws, Configured = plan.Node.ToString().Contains(".Configure()", StringComparison.Ordinal),
            }
            : plan;
    }

    private static MockCall PlanAfterCall(
        InvocationExpressionSyntax setup, ExpressionStatementSyntax statement, InvocationExpressionSyntax? returns, bool verifiable, MockCall call, ArgumentSyntax lambda, SemanticModel model)
    {

        if (returns is null)
        {
            return call.Type?.SpecialType == SpecialType.System_Void
                ? new() { Removed = true, Member = call.Member, ForAnyArguments = call.ForAnyArguments, Verifiable = verifiable }
                : MockCall.Blocked(MoqReason.SetupShape, "a Setup of a member with a value but without Returns answers Moq's default; not yet supported");
        }

        string name = ((MemberAccessExpressionSyntax)returns.Expression).Name.Identifier.Text;
        if (name is "Throws" or "ThrowsAsync")
        {
            return Throws(returns, name, (SimpleLambdaExpressionSyntax)lambda.Expression, call, statement, verifiable, model);
        }

        if (returns.ArgumentList.Arguments is not [{ Expression: var value }]
            || model.GetSymbolInfo(returns).Symbol is not IMethodSymbol { Parameters: [var parameter] })
        {
            return MockCall.Blocked(MoqReason.ReturnsLambda, $"{name} with several arguments (a delay or a function with more arguments)");
        }

        // Row 7: a function computes the value per call, in both libraries.
        // Moq also has Returns(Delegate), which a method group binds to.
        bool function = (parameter.Type.TypeKind == TypeKind.Delegate || parameter.Type.SpecialType == SpecialType.System_Delegate)
            && !SymbolEqualityComparer.Default.Equals(parameter.Type, call.Type);
        if (function && call.Type!.TypeKind == TypeKind.Delegate)
        {
            return MockCall.Blocked(MoqReason.ReturnsLambda, $"{name} with a function on a member that itself returns a delegate");
        }

        if (function && value is not LambdaExpressionSyntax)
        {
            return MockCall.Blocked(MoqReason.ReturnsLambda, $"{name} with a method group or anonymous method; only a plain lambda is rewritten");
        }

        ExpressionSyntax returned = value.WithoutTrivia();
        LambdaExpressionSyntax? converted = null;
        if (function)
        {
            IReadOnlyList<IParameterSymbol> mocked = call.Member is IMethodSymbol method ? method.Parameters : [];
            MockCall lambdaCall = CallLambda.Convert(value, mocked, model);
            if (lambdaCall.Blocker is not null)
            {
                return lambdaCall;
            }

            converted = (LambdaExpressionSyntax)lambdaCall.Node!;
            returned = converted;
        }

        bool needsTasks = false;
        if (name == "ReturnsAsync")
        {
            if (call.Type is not INamedTypeSymbol { TypeArguments: [var result] } task
                || task.OriginalDefinition.ToDisplayString() is not ("System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>"))
            {
                return MockCall.Blocked(MoqReason.ReturnsLambda, "ReturnsAsync on a member that is not Task<T>/ValueTask<T>");
            }

            string resultType = result.ToMinimalDisplayString(model, setup.SpanStart);
            string Wrap(string inner) => task.Name == "Task" ? $"Task.FromResult<{resultType}>({inner})" : $"new ValueTask<{resultType}>({inner})";
            if (converted is SimpleLambdaExpressionSyntax { ExpressionBody: { } body } simple)
            {
                returned = simple.WithExpressionBody(SyntaxFactory.ParseExpression(Wrap(body.ToString())));
            }
            else if (converted is not null)
            {
                return MockCall.Blocked(MoqReason.ReturnsLambda, "ReturnsAsync with a block-bodied function; only an expression body is rewritten");
            }
            else
            {
                returned = SyntaxFactory.ParseExpression(Wrap(returned.ToString()));
            }

            needsTasks = true;
        }

        ExpressionStatementSyntax rewritten = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, (ExpressionSyntax)call.Node!, SyntaxFactory.IdentifierName("Returns")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(returned)))));
        return new()
        {
            Node = rewritten.WithTriviaFrom(statement),
            Member = call.Member,
            ForAnyArguments = call.ForAnyArguments,
            Type = needsTasks ? call.Type : null,
            Verifiable = verifiable,
        };
    }

    // Row 6: `.Throws(ex)` / `.Throws<E>()` → NSubstitute.ExceptionExtensions `.Throws(ex)` / `.Throws<E>()`, `.ThrowsAsync(ex)` likewise; a void
    // member has no value to configure, so it becomes `m.Object.When(x => x.M(..)).Do(_ => throw ex);`. There the exception is created per
    // call, so only a variable, a field or `new E(..)` (a fresh instance the test cannot hold) is accepted.
    private static MockCall Throws(
        InvocationExpressionSyntax throws, string name, SimpleLambdaExpressionSyntax lambda, MockCall call, ExpressionStatementSyntax statement, bool verifiable, SemanticModel model)
    {
        var access = (MemberAccessExpressionSyntax)throws.Expression;
        SeparatedSyntaxList<ArgumentSyntax> arguments = throws.ArgumentList.Arguments;
        bool generic = access.Name is GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } && arguments.Count == 0;
        if (!generic && (arguments is not [{ Expression: var thrown }] || model.GetTypeInfo(thrown).Type is not { } type || !IsException(type)))
        {
            return MockCall.Blocked(MoqReason.ThrowsShape, $"{name} with a function or several arguments; only {name}(exception) or {name}<E>()");
        }

        bool isVoid = call.Type?.SpecialType == SpecialType.System_Void;
        StatementSyntax rewritten;
        if (!isVoid)
        {
            rewritten = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, (ExpressionSyntax)call.Node!, access.Name.WithoutTrivia()),
                throws.ArgumentList.WithoutTrivia()));
        }
        else if (name == "ThrowsAsync")
        {
            return MockCall.Blocked(MoqReason.ThrowsShape, "ThrowsAsync on a void member");
        }
        else
        {
            string exception = generic ? $"new {((GenericNameSyntax)access.Name).TypeArgumentList.Arguments[0]}()" : arguments[0].Expression.WithoutTrivia().ToString();
            if (!generic && arguments[0].Expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or ObjectCreationExpressionSyntax))
            {
                return MockCall.Blocked(MoqReason.ThrowsShape, "Throws on a void member with an exception expression that may differ per call; use a variable or new E(..)");
            }

            string parameter = lambda.Parameter.Identifier.Text;
            var when = MockCall.Rewrite(lambda, SyntaxFactory.IdentifierName(parameter), model);
            if (when.Blocker is not null)
            {
                return when;
            }

            // When(..) is a specification, not a call: it needs no Configure().
            ExpressionSyntax mock = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)lambda.Parent!.Parent!.Parent!).Expression).Expression;
            rewritten = SyntaxFactory.ParseStatement($"{mock.WithoutTrivia()}.Object.When({parameter} => {when.Node}).Do(_ => throw {exception});");
        }

        return new()
        {
            Node = rewritten.WithTriviaFrom(statement),
            Member = call.Member,
            ForAnyArguments = call.ForAnyArguments,
            Verifiable = verifiable,
            Throws = !isVoid,
        };
    }

    private static bool IsException(ITypeSymbol type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Exception")
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasOtherSetup(InvocationExpressionSyntax setup, SemanticModel model)
    {
        if (setup.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || MockUsage.Holder(receiver, model) is not { } holder
            || SetupMember(setup, model) is not { } member)
        {
            return false;
        }

        SyntaxNode? scope = holder is IFieldSymbol field
            ? field.ContainingType.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
            : holder.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().FirstAncestorOrSelf<MemberDeclarationSyntax>();
        return (scope ?? setup.SyntaxTree.GetRoot()).DescendantNodes().OfType<InvocationExpressionSyntax>().Any(other =>
            other != setup && (IsSetup(other, model) || SetupSequenceSyntax.IsSequence(other, model))
            && other.Expression is MemberAccessExpressionSyntax { Expression: var otherReceiver }
            && SymbolEqualityComparer.Default.Equals(MockUsage.Holder(otherReceiver, model), holder)
            && SymbolEqualityComparer.Default.Equals(SetupMember(other, model), member));
    }

    private static ISymbol? SetupMember(InvocationExpressionSyntax setup, SemanticModel model) =>
        setup.ArgumentList.Arguments is [{ Expression: SimpleLambdaExpressionSyntax { ExpressionBody: { } body } }] ? model.GetSymbolInfo(body).Symbol : null;

    private static SetupChain Chain(InvocationExpressionSyntax setup)
    {
        ExpressionSyntax current = setup;
        InvocationExpressionSyntax? returns = null;
        InvocationExpressionSyntax? callback = null;
        bool callbackFirst = false;
        if (Next(current, "Callback") is { } first)
        {
            callback = first;
            callbackFirst = true;
            current = first;
        }

        if (Next(current, "Returns", "ReturnsAsync", "Throws", "ThrowsAsync") is { } next)
        {
            returns = next;
            current = next;
        }

        if (callback is null && Next(current, "Callback") is { } after)
        {
            callback = after;
            current = after;
        }

        bool verifiable = false;
        if (Next(current, "Verifiable") is { ArgumentList.Arguments.Count: 0 } verified)
        {
            verifiable = true;
            current = verified;
        }

        return current.Parent is ExpressionStatementSyntax statement
            ? new SetupChain { Statement = statement, Returns = returns, Callback = callback, CallbackFirst = callbackFirst, Verifiable = verifiable }
            : new SetupChain();
    }

    private static InvocationExpressionSyntax? Next(ExpressionSyntax current, params string[] names) =>
        current.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } access
        && access.Expression == current && names.Contains(access.Name.Identifier.Text) && invocation.Expression == access ? invocation : null;
}
