using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Splits `x.Should().A().And.B();` into `x.Should().A(); x.Should().B();` and lets the link patterns (Composite)
// rewrite each statement on a speculative semantic model of the split tree. One unsupported link makes the chain Manual.
public class AndChainPattern : IPattern
{
    private readonly IReadOnlyList<IPattern> _links;

    public AndChainPattern(IReadOnlyList<IPattern> links)
    {
        ArgumentNullException.ThrowIfNull(links);
        _links = links;
    }

    public string Id => "fa.and-chain";

    public string Description => "x.Should().A().And.B() → x.ShouldA(); x.ShouldB();";

    // The statement holding the chain, or the outermost link when the chain is not a statement (reported as Manual).
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => GetPrevious(i) is not null
                && i.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "And" }
                && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(i).Symbol))
            .Select<InvocationExpressionSyntax, SyntaxNode>(i => i.Parent is ExpressionStatementSyntax statement ? statement : i);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        if (FluentAssertionsSyntax.IsInsideExpectedFailure(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.ExpectedFailureReason);
        }

        if (node is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax top } statement)
        {
            return PatternResult.Manual("The result of the .And chain is used (.Which or a value); Shouldly's methods return nothing.");
        }

        if (!FluentAssertionsSyntax.TryGetSplittableStatement(statement, out _))
        {
            return PatternResult.Manual("The .And chain cannot be split: " + FluentAssertionsSyntax.NotSplittableReason);
        }

        // Walk down to `x.Should()`; links ends up in source order.
        var links = new List<InvocationExpressionSyntax> { top };
        while (GetPrevious(links[0]) is { } previous)
        {
            links.Insert(0, previous);
        }

        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(links[0]);
        if (subject is null)
        {
            return PatternResult.Manual("The .And chain does not start with x.Should().Method(...).");
        }

        // Every `.And` must be the assertions on x again: a same-named method on another subject would bind after splitting.
        var should = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)links[0].Expression).Expression;
        ITypeSymbol? shouldType = model.GetTypeInfo(should).Type;
        if (links.Skip(1).Any(l => !SymbolEqualityComparer.Default.Equals(
            model.GetTypeInfo(((MemberAccessExpressionSyntax)l.Expression).Expression).Type, shouldType)))
        {
            return PatternResult.Manual("An .And in the chain continues on another subject than x.Should(): rewrite it by hand.");
        }

        // One FluentAssertions statement per link, rewritten by the link patterns on the split tree. A subject that must
        // not run twice goes into a local first.
        var annotations = new List<SyntaxAnnotation>();
        var split = new List<StatementSyntax>();
        if (!FluentAssertionsSyntax.IsSafeToEvaluateTwice(subject, model))
        {
            (StatementSyntax declaration, IdentifierNameSyntax local) = FluentAssertionsSyntax.Hoist(subject, model);
            split.Add(declaration);
            should = should.ReplaceNode(subject, local);
        }

        foreach (InvocationExpressionSyntax link in links)
        {
            var annotation = new SyntaxAnnotation();
            annotations.Add(annotation);
            split.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        should.WithoutTrivia(),
                        ((MemberAccessExpressionSyntax)link.Expression).Name.WithoutTrivia()),
                    link.ArgumentList.WithoutTrailingTrivia()))
                .WithAdditionalAnnotations(annotation));
        }

        SyntaxTree tree = model.SyntaxTree;
        SyntaxTree splitTree = tree.WithRootAndOptions(tree.GetRoot().ReplaceNode(statement, split), tree.Options);
        SemanticModel splitModel = model.Compilation.ReplaceSyntaxTree(tree, splitTree).GetSemanticModel(splitTree);
        SyntaxNode splitRoot = splitTree.GetRoot();

        List<SyntaxNode> statements = [.. split.Take(split.Count - links.Count)];
        var namespaces = new List<string>();
        var warnings = new List<string>();
        for (int i = 0; i < links.Count; i++)
        {
            SyntaxNode linkStatement = splitRoot.GetAnnotatedNodes(annotations[i]).Single();
            string name = ((MemberAccessExpressionSyntax)links[i].Expression).Name.ToString();
            PatternResult? result = RewriteLink(linkStatement, splitModel);
            switch (result)
            {
                // A link can become several statements itself (OnlyContain, AllSatisfy).
                case FixedResult { NewNodes: var nodes } fixedResult when nodes.All(n => n is StatementSyntax):
                    statements.AddRange(nodes.Select(n => n.WithoutTrivia()));
                    namespaces.AddRange(fixedResult.RequiredNamespaces);
                    if (fixedResult.Warning is not null)
                    {
                        warnings.Add(fixedResult.Warning);
                    }

                    break;
                case ManualResult manual:
                    return PatternResult.Manual($"Link {name} of the .And chain: {manual.Reason}");
                default:
                    return PatternResult.Manual($"The .And chain contains {name}, which no pattern rewrites: fix the chain by hand.");
            }
        }

        return new FixedResult(statements, [.. namespaces.Distinct(StringComparer.Ordinal)])
        {
            Warning = warnings.Count == 0 ? null : string.Join(" ", warnings),
        };
    }

    // The first link pattern that finds something in the statement rewrites it; the result replaces that node.
    // Find runs on the statement's parent, not the whole file: patterns that find the statement itself need it as a descendant.
    private PatternResult? RewriteLink(SyntaxNode statement, SemanticModel model)
    {
        foreach (IPattern pattern in _links)
        {
            SyntaxNode? found = pattern.Find(statement.Parent!, model).FirstOrDefault(n => statement.Span.Contains(n.Span));
            if (found is null)
            {
                continue;
            }

            PatternResult result = pattern.Rewrite(found, model);
            if (result is not FixedResult fixedResult)
            {
                return result;
            }

            if (found == statement)
            {
                return fixedResult;
            }

            return fixedResult.NewNodes is [{ } newNode]
                ? fixedResult with { NewNodes = [statement.ReplaceNode(found, newNode.WithTriviaFrom(found))] }
                : null;
        }

        return null;
    }

    // The link before `.And.Method(...)`, or null when the invocation does not continue a chain.
    private static InvocationExpressionSyntax? GetPrevious(InvocationExpressionSyntax link) =>
        link.Expression is MemberAccessExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "And", Expression: InvocationExpressionSyntax previous },
        }
            ? previous
            : null;
}
