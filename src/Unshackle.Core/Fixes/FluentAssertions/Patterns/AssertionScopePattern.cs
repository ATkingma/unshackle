using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Runs before the assertion patterns: it turns a scope that holds only assertion statements into
// `x.ShouldSatisfyAllConditions(() => a, () => b);`, whose lambdas those patterns then rewrite. Both collect every
// failure, also an exception that is no assertion (probed); ShouldSatisfyAllConditions only prints its receiver.
public class AssertionScopePattern : IPattern
{
    private const string ManualReason =
        "AssertionScope collects all failures; Shouldly has no scope. Rewrite the block to x.ShouldSatisfyAllConditions(() => ..., () => ...) and remove the scope.";

    public string Id => "fa.assertion-scope";

    public string Description => "using (new AssertionScope()) { a; b; } → x.ShouldSatisfyAllConditions(() => a, () => b); otherwise Manual";

    // The using statement, the block of a `using var` scope (its statements are replaced), else the creation (Manual).
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<BaseObjectCreationExpressionSyntax>()
            .Where(c => FluentAssertionsSyntax.IsAssertionScope(model.GetTypeInfo(c).Type))
            .Select<BaseObjectCreationExpressionSyntax, SyntaxNode>(c => c.Parent switch
            {
                UsingStatementSyntax u when u.Expression == c => u,
                EqualsValueClauseSyntax { Parent.Parent.Parent: LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0, Parent: BlockSyntax block } } => block,
                _ => c,
            });

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case UsingStatementSyntax { Expression: BaseObjectCreationExpressionSyntax creation, Statement: BlockSyntax body } usingStatement
                when IsPlain(creation) && TryConvert(body.Statements, usingStatement, model, out StatementSyntax? converted):
                return PatternResult.Fixed(converted);
            case BlockSyntax block:
                List<LocalDeclarationStatementSyntax> scopes =
                [
                    .. block.Statements.OfType<LocalDeclarationStatementSyntax>()
                        .Where(d => d.UsingKeyword != default && d.Declaration.Variables.Any(v => v.Initializer?.Value is BaseObjectCreationExpressionSyntax c
                            && FluentAssertionsSyntax.IsAssertionScope(model.GetTypeInfo(c).Type))),
                ];
                if (scopes is [{ Declaration.Variables: [{ Initializer.Value: BaseObjectCreationExpressionSyntax plain } variable] } scope] && IsPlain(plain))
                {
                    int index = block.Statements.IndexOf(scope);
                    List<StatementSyntax> rest = [.. block.Statements.Skip(index + 1)];
                    if (!rest.SelectMany(s => s.DescendantTokens()).Any(t => t.ValueText == variable.Identifier.ValueText)
                        && TryConvert(rest, scope, model, out StatementSyntax? convertedRest))
                    {
                        return PatternResult.Fixed(block.WithStatements([.. block.Statements.Take(index), convertedRest.WithTriviaFrom(scope)]));
                    }
                }

                return PatternResult.Manual(ManualReason);
            default:
                return PatternResult.Manual(ManualReason);
        }
    }

    // `new AssertionScope()` or `new()`: a context name or a strategy has no Shouldly counterpart.
    private static bool IsPlain(BaseObjectCreationExpressionSyntax creation) =>
        creation.ArgumentList is null or { Arguments.Count: 0 } && creation.Initializer is null;

    // Every statement must be an awaited-free FluentAssertions assertion statement (no declarations, other calls,
    // Discard or captured ref locals): those would change meaning inside a lambda.
    private static bool TryConvert(IReadOnlyList<StatementSyntax> statements, StatementSyntax replaced, SemanticModel model, [NotNullWhen(true)] out StatementSyntax? converted)
    {
        converted = null;
        ExpressionSyntax? receiver = null;
        foreach (StatementSyntax statement in statements)
        {
            if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation }
                || !FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(invocation).Symbol)
                || invocation.DescendantNodes().Any(n => n is AwaitExpressionSyntax
                    || n is IdentifierNameSyntax && model.GetSymbolInfo(n).Symbol is IParameterSymbol { RefKind: not RefKind.None } or ILocalSymbol { IsRef: true }))
            {
                return false;
            }

            receiver ??= invocation.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Select(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Should" } should && i.ArgumentList.Arguments.Count == 0 ? should.Expression : null)
                .LastOrDefault(e => e is not null && FluentAssertionsSyntax.IsSafeToEvaluateTwice(e, model));
        }

        if (statements.Count == 0 || receiver is null)
        {
            return false;
        }

        string indent = new([.. replaced.GetLeadingTrivia().Reverse().TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)).Reverse().SelectMany(t => t.ToString())]);
        string eol = replaced.SyntaxTree.GetText().ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        // Block bodies: during the fix Shouldly is not referenced yet, so an expression lambda's delegate type is unknown and
        // the assertion patterns could not tell its value is discarded. A statement also lets AndChainPattern split a chain.
        IEnumerable<string> lambdas = statements.Cast<ExpressionStatementSyntax>().Select(s => $"{eol}{indent}    () => {{ {s.Expression.WithoutTrivia()}; }}");
        converted = SyntaxFactory.ParseStatement($"{receiver.WithoutTrivia()}.ShouldSatisfyAllConditions({string.Join(",", lambdas)});");
        return true;
    }
}
