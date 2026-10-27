using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// <c>scenarioContext.Pending();</c> (the step skeleton SpecFlow generates) → <c>throw new PendingStepException();</c>. Reqnroll marks
/// <c>Pending</c> and <c>StepIsPending</c> obsolete; both only throw that exception in SpecFlow 3.9. Only as the last statement (no
/// unreachable code after it) and on a receiver without side effects; otherwise Manual.
/// </summary>
public class PendingPattern : IPattern
{
    private static readonly HashSet<string> s_methods = new(["Pending", "StepIsPending"], StringComparer.Ordinal);

    public string Id => "specflow.pending";

    public string Description => "scenarioContext.Pending() → throw new PendingStepException()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol method
                && s_methods.Contains(method.Name)
                && SpecFlowNames.FullName(method.ContainingType) == SpecFlowNames.OldRoot + ".ScenarioContext")
            .Select(i => i.Parent is ExpressionStatementSyntax statement ? statement : (SyntaxNode)i);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        InvocationExpressionSyntax invocation = node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax call } ? call : (InvocationExpressionSyntax)node;
        if (invocation.ArgumentList.Arguments.Count > 0 || !IsPlainReceiver(invocation.Expression, model))
        {
            return PatternResult.Manual("Pending() on a receiver with possible side effects; replace it with throw new PendingStepException() by hand.");
        }

        return invocation.Parent switch
        {
            ExpressionStatementSyntax { Parent: BlockSyntax block } statement when block.Statements.Last() == statement =>
                PatternResult.Fixed(SyntaxFactory.ParseStatement("throw new PendingStepException();"), SpecFlowNames.NewRoot),
            ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax or LocalFunctionStatementSyntax } => PatternResult.Fixed(SyntaxFactory.ParseExpression("throw new PendingStepException()"), SpecFlowNames.NewRoot),
            _ => PatternResult.Manual("Pending() is not the last statement of its block (a throw there would make the rest unreachable); " +
                "replace it with throw new PendingStepException() by hand."),
        };
    }

    // ctx.Pending(), this.ctx.Pending(), ScenarioContext.Current.Pending(), ScenarioContext.StepIsPending(): reading them has no side effect.
    private static bool IsPlainReceiver(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        IdentifierNameSyntax => true,
        MemberAccessExpressionSyntax access => access.Expression switch
        {
            IdentifierNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax => true,
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Current" } current =>
                model.GetSymbolInfo(current.Expression).Symbol is INamedTypeSymbol type && SpecFlowNames.IsOldType(type),
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax } => true,
            _ => false,
        },
        _ => false,
    };
}
