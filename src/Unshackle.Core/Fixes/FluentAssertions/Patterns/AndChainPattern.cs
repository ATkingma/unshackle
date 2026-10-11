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

        if (node is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax top } statement)
        {
            return PatternResult.Manual("The result of the .And chain is used (.Which or a value); Shouldly's methods return nothing.");
        }

        if (statement.Parent is not (BlockSyntax or SwitchSectionSyntax))
        {
            return PatternResult.Manual("The .And chain cannot be split: the statement is not directly in a block.");
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

        if (!IsFreeOfSideEffects(subject, model))
        {
            return PatternResult.Manual("The .And chain subject is not a local, parameter, field or property, so splitting would evaluate it twice.");
        }

        // One FluentAssertions statement per link, rewritten by the link patterns on the split tree.
        var annotations = new List<SyntaxAnnotation>();
        var split = new List<StatementSyntax>();
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

        var statements = new List<SyntaxNode>();
        var namespaces = new List<string>();
        for (int i = 0; i < links.Count; i++)
        {
            SyntaxNode linkStatement = splitRoot.GetAnnotatedNodes(annotations[i]).Single();
            string name = ((MemberAccessExpressionSyntax)links[i].Expression).Name.ToString();
            PatternResult? result = RewriteLink(linkStatement, splitRoot, splitModel);
            switch (result)
            {
                case FixedResult { NewNodes: [StatementSyntax rewritten] } fixedResult:
                    statements.Add(rewritten.WithoutTrivia());
                    namespaces.AddRange(fixedResult.RequiredNamespaces);
                    break;
                case ManualResult manual:
                    return PatternResult.Manual($"Link {name} of the .And chain: {manual.Reason}");
                default:
                    return PatternResult.Manual($"The .And chain contains {name}, which no pattern rewrites: fix the chain by hand.");
            }
        }

        return PatternResult.Fixed(statements, [.. namespaces.Distinct(StringComparer.Ordinal)]);
    }

    // The first link pattern that finds something in the statement rewrites it; the result replaces that node.
    private PatternResult? RewriteLink(SyntaxNode statement, SyntaxNode root, SemanticModel model)
    {
        foreach (IPattern pattern in _links)
        {
            SyntaxNode? found = pattern.Find(root, model).FirstOrDefault(n => statement.Span.Contains(n.Span));
            if (found is null)
            {
                continue;
            }

            PatternResult result = pattern.Rewrite(found, model);
            if (result is not FixedResult { NewNodes: [{ } newNode] } fixedResult)
            {
                return result is ManualResult ? result : null;
            }

            SyntaxNode rewritten = found == statement ? newNode : statement.ReplaceNode(found, newNode.WithTriviaFrom(found));
            return fixedResult with { NewNodes = [rewritten] };
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

    // Conservative: `this`, locals, parameters, fields and properties without arguments, and member accesses on those.
    private static bool IsFreeOfSideEffects(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        ThisExpressionSyntax => true,
        IdentifierNameSyntax => IsStorage(model.GetSymbolInfo(expression).Symbol),
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax } access =>
            IsStorage(model.GetSymbolInfo(access).Symbol)
            && (model.GetSymbolInfo(access.Expression).Symbol is INamedTypeSymbol || IsFreeOfSideEffects(access.Expression, model)),
        _ => false,
    };

    private static bool IsStorage(ISymbol? symbol) =>
        symbol is ILocalSymbol or IParameterSymbol or IFieldSymbol or IPropertySymbol { IsIndexer: false };
}
