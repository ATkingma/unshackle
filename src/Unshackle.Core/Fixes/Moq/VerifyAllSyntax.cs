using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// Council round 8, M4 (moq-queue.md row 10): `m.Verify();` (the .Verifiable() setups) and `m.VerifyAll();` (every setup) expand to one
// `m.Object.Received().M(..);` per covered setup, only when the mock is a local whose every setup is a plain statement in the same block
// as the Verify, before it (no constructor, helper, field, condition or loop). Two covered setups of one member are Manual: Moq matches
// a call to the latest setup only, so the earlier one counts as not called, while NSubstitute's Received sees every matching call.
// VerifyNoOtherCalls is Manual.
public static class VerifyAllSyntax
{
    public static bool IsVerifyAll(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation is { Parent: ExpressionStatementSyntax, ArgumentList.Arguments.Count: 0, Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Verify" or "VerifyAll" or "VerifyNoOtherCalls" } }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: false } method
        && (MoqSyntax.IsMockOfT(method.ContainingType) || MoqSyntax.IsMockClass(method.ContainingType));

    public static VerifyAllPlan Plan(InvocationExpressionSyntax verify, SemanticModel model)
    {
        var access = (MemberAccessExpressionSyntax)verify.Expression;
        string name = access.Name.Identifier.Text;
        if (name == "VerifyNoOtherCalls")
        {
            return VerifyAllPlan.Blocked(MoqReason.VerifyNoOtherCalls, "VerifyNoOtherCalls has no exact NSubstitute form (council round 8, M4)");
        }

        if (MockUsage.Holder(access.Expression, model) is not ILocalSymbol mock
            || verify.Parent?.Parent is not BlockSyntax block
            || block.Parent is not (BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax or AccessorDeclarationSyntax))
        {
            return NotLocal(name, "the mock is not a local, or the Verify is not a plain statement of a method body");
        }

        SyntaxNode scope = (SyntaxNode?)mock.DeclaringSyntaxReferences[0].GetSyntax().FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? block;
        var covered = new List<(InvocationExpressionSyntax Setup, ExpressionStatementSyntax Statement)>();
        foreach (InvocationExpressionSyntax setup in scope.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => SetupReturnsSyntax.IsSetup(i, model) && i.Expression is MemberAccessExpressionSyntax { Expression: var r }
                && SymbolEqualityComparer.Default.Equals(MockUsage.Holder(r, model), mock)))
        {
            if (SetupReturnsSyntax.Statement(setup) is not { } statement || statement.Parent != block || statement.SpanStart > verify.SpanStart)
            {
                return NotLocal(name, $"a setup of {mock.Name} is not a plain statement before it in the same block");
            }

            if (name == "VerifyAll" || SetupReturnsSyntax.IsVerifiable(setup))
            {
                covered.Add((setup, statement));
            }
        }

        var statements = new List<StatementSyntax>();
        var members = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach ((InvocationExpressionSyntax setup, _) in covered)
        {
            var receiver = (MemberAccessExpressionSyntax)setup.Expression;
            var call = MockCall.Rewrite(setup.ArgumentList.Arguments[0].Expression,
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseExpression($"{receiver.Expression.WithoutTrivia()}.Object.Received()"), model);
            if (call.Blocker is not null)
            {
                return new() { Blocker = call.Blocker };
            }

            if (!members.Add(call.Member!))
            {
                return VerifyAllPlan.Blocked(MoqReason.SetupsOverlap,
                    $"{name} covers two setups of {call.Member!.Name}: Moq counts a call only for the latest matching setup, NSubstitute's Received for every one");
            }

            statements.Add(call.Node is InvocationExpressionSyntax invocation
                ? Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ExpressionStatement(invocation)
                : Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseStatement($"_ = {call.Node};"));
        }

        string lines = string.Join(", ", covered.Select(c => c.Statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
        return new()
        {
            Statements = statements,
            Warning = $"{name}() expanded into {statements.Count} Received() check(s) for the setup(s) at line(s) {(lines.Length == 0 ? "none" : lines)} (council round 8, M4)",
        };
    }

    private static VerifyAllPlan NotLocal(string name, string why) =>
        VerifyAllPlan.Blocked(MoqReason.VerifyScopeNotLocal, $"{name}(): {why} (council round 8, M4)");
}
