using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Splits `x.Should().A().And.B();` into `x.Should().A(); x.Should().B();` and lets the link patterns (Composite)
// rewrite each statement on a speculative semantic model of the split tree. One unsupported link makes the chain Manual.
// The same split serves assertions that Shouldly has no single call for (TAAK-171): ContainKeys(a, b) becomes one
// ContainKey per key, ContainValue(s) become Contain on x.Values, and `x.Should().ContainKey(k).WhoseValue.Should().M()`
// becomes `x.Should().ContainKey(k); x[k].Should().M();`.
public class AndChainPattern : IPattern
{
    // FluentAssertions dictionary assertions that several Shouldly statements replace: the FluentAssertions method each
    // statement uses, whether it asserts on x.Values, and whether every params argument gets a statement of its own.
    private static readonly Dictionary<string, (string Target, bool OnValues, bool PerArgument)> s_expansions = new(StringComparer.Ordinal)
    {
        ["ContainKeys"] = ("ContainKey", false, true),
        ["NotContainKeys"] = ("NotContainKey", false, true),
        ["ContainValues"] = ("Contain", true, true),
        ["NotContainValues"] = ("NotContain", true, true),
        ["ContainValue"] = ("Contain", true, false),
        ["NotContainValue"] = ("NotContain", true, false),
    };

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
            .Where(i => i.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "And" }
                && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(i).Symbol)
                && (GetPrevious(i) is not null || IsExpandable(i, model)))
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
        // not run twice goes into a local first. `x.Should().ContainKey(k).WhoseValue.Should()` first asserts the key,
        // then continues on x[k]; x is the subject that must not run twice, and k is read twice.
        var entries = new List<(ExpressionSyntax Expression, string Name)>();
        var split = new List<StatementSyntax>();
        InvocationExpressionSyntax? keyAssertion = GetWhoseValueSource(subject, model);
        ExpressionSyntax dictionary = keyAssertion is null ? subject : FluentAssertionsSyntax.GetSubject(keyAssertion)!;
        bool maybeNull = model.GetTypeInfo(dictionary).Nullability.FlowState == NullableFlowState.MaybeNull;
        ExpressionSyntax receiver = dictionary.WithoutTrivia();
        if (!FluentAssertionsSyntax.IsSafeToEvaluateTwice(dictionary, model))
        {
            (StatementSyntax declaration, IdentifierNameSyntax local) = FluentAssertionsSyntax.Hoist(dictionary, model);
            split.Add(declaration);
            receiver = local;
        }

        if (keyAssertion is null)
        {
            should = should.ReplaceNode(subject, receiver);
        }
        else
        {
            if (keyAssertion.ArgumentList.Arguments is not [{ NameColon: null } key, ..])
            {
                return PatternResult.Manual("ContainKey(...).WhoseValue with a named key argument: rewrite it by hand.");
            }

            ITypeSymbol? dictionaryType = model.GetTypeInfo(dictionary).Type;
            if (dictionaryType is null or IErrorTypeSymbol || !HasIndexer(dictionaryType, model.GetTypeInfo(key.Expression).Type, model.Compilation))
            {
                return PatternResult.Manual($"ContainKey(...).WhoseValue on {dictionaryType?.ToDisplayString()}: the type has no indexer by the key, so the value cannot be read after ShouldContainKey; assert by hand.");
            }

            if (!model.GetConstantValue(key.Expression).HasValue && !FluentAssertionsSyntax.IsSafeToEvaluateTwice(key.Expression, model))
            {
                return PatternResult.Manual("ContainKey(...).WhoseValue with a key that is computed: the key would be evaluated twice; put it in a local first.");
            }

            entries.Add((FluentAssertionsSyntax.Invoke(
                FluentAssertionsSyntax.Invoke(receiver, "Should"), "ContainKey", keyAssertion.ArgumentList.Arguments), "ContainKey"));
            ElementAccessExpressionSyntax element = SyntaxFactory.ElementAccessExpression(
                receiver is IdentifierNameSyntax or MemberAccessExpressionSyntax ? receiver : SyntaxFactory.ParenthesizedExpression(receiver),
                SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(key.Expression.WithoutTrivia()))));
            should = should.ReplaceNode(subject, element);
            maybeNull = false;
        }

        foreach (InvocationExpressionSyntax link in links)
        {
            string name = ((MemberAccessExpressionSyntax)link.Expression).Name.Identifier.ValueText;
            if (s_expansions.TryGetValue(name, out (string Target, bool OnValues, bool PerArgument) expansion)
                && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(link).Symbol))
            {
                string? reason = Expand(link, expansion, should, model.GetTypeInfo(subject).Type, maybeNull, model, entries);
                if (reason is not null)
                {
                    return PatternResult.Manual($"Link {name} of the .And chain: {reason}");
                }

                continue;
            }

            entries.Add((SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    should.WithoutTrivia(),
                    ((MemberAccessExpressionSyntax)link.Expression).Name.WithoutTrivia()),
                link.ArgumentList.WithoutTrailingTrivia()), name));
        }

        var annotations = new List<SyntaxAnnotation>();
        int prefix = split.Count;
        foreach ((ExpressionSyntax expression, _) in entries)
        {
            var annotation = new SyntaxAnnotation();
            annotations.Add(annotation);
            split.Add(SyntaxFactory.ExpressionStatement(expression).WithAdditionalAnnotations(annotation));
        }

        SyntaxTree tree = model.SyntaxTree;
        SyntaxTree splitTree = tree.WithRootAndOptions(tree.GetRoot().ReplaceNode(statement, split), tree.Options);
        SemanticModel splitModel = model.Compilation.ReplaceSyntaxTree(tree, splitTree).GetSemanticModel(splitTree);
        SyntaxNode splitRoot = splitTree.GetRoot();

        List<SyntaxNode> statements = [.. split.Take(prefix)];
        var namespaces = new List<string>();
        var warnings = new List<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            SyntaxNode linkStatement = splitRoot.GetAnnotatedNodes(annotations[i]).Single();
            string name = entries[i].Name;
            PatternResult? result = RewriteLink(linkStatement, splitModel);
            switch (result)
            {
                // A link can become several statements itself (OnlyContain, AllSatisfy).
                case FixedResult { NewNodes: var nodes } fixedResult when nodes.All(n => n is StatementSyntax):
                    // NotBeEmpty().And.OnlyContain(p) would assert ShouldNotBeEmpty twice: skip a repeat of the previous statement.
                    foreach (SyntaxNode newStatement in nodes.Select(n => n.WithoutTrivia()))
                    {
                        if (statements.Count == 0 || !IsSameStatement(statements[^1], newStatement))
                        {
                            statements.Add(newStatement);
                        }
                    }

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

    // Rewritten nodes from different link patterns differ in trivia and token details, so compare the normalized text.
    private static bool IsSameStatement(SyntaxNode first, SyntaxNode second) =>
        string.Equals(first.NormalizeWhitespace().ToFullString(), second.NormalizeWhitespace().ToFullString(), StringComparison.Ordinal);

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

    // Statements for a link that FluentAssertions' dictionary assertions spread over several Shouldly calls. Returns the
    // Manual reason, or null after adding the statements.
    private static string? Expand(
        InvocationExpressionSyntax link,
        (string Target, bool OnValues, bool PerArgument) expansion,
        InvocationExpressionSyntax should,
        ITypeSymbol? subjectType,
        bool maybeNull,
        SemanticModel model,
        List<(ExpressionSyntax Expression, string Name)> entries)
    {
        string name = ((MemberAccessExpressionSyntax)link.Expression).Name.Identifier.ValueText;
        if (link.ArgumentList.Arguments.Any(a => a.NameColon is not null))
        {
            return $"{name}(...) with named arguments: rewrite it by hand.";
        }

        if (expansion.PerArgument
            && (link.ArgumentList.Arguments.Count == 0 || !IsExpandedParams(link, model)))
        {
            return $"{name}(...) with a collection argument (or becauseArgs), not a list of {(name.EndsWith("Keys", StringComparison.Ordinal) ? "keys" : "values")}: rewrite it by hand.";
        }

        // should is built from new nodes: the semantic model knows only the original subject, whose type it is on.
        ExpressionSyntax subject = ((MemberAccessExpressionSyntax)should.Expression).Expression;
        InvocationExpressionSyntax target = should;
        if (expansion.OnValues)
        {
            if (subjectType is null or IErrorTypeSymbol || !HasValues(subjectType))
            {
                return $"{name}(...) on {subjectType?.ToDisplayString()}: the type has no Values property to assert on; rewrite it by hand.";
            }

            ExpressionSyntax receiver = maybeNull
                ? SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, subject.WithoutTrivia())
                : subject.WithoutTrivia();
            target = FluentAssertionsSyntax.Invoke(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName("Values")), "Should");
        }

        IEnumerable<IEnumerable<ArgumentSyntax>> argumentLists = expansion.PerArgument
            ? link.ArgumentList.Arguments.Select(a => (IEnumerable<ArgumentSyntax>)[a])
            : [link.ArgumentList.Arguments];
        foreach (IEnumerable<ArgumentSyntax> arguments in argumentLists)
        {
            entries.Add((FluentAssertionsSyntax.Invoke(target.WithoutTrivia(), expansion.Target, arguments), expansion.Target));
        }

        return null;
    }

    // The call bound to the params TKey[]/TValue[] overload with its arguments listed, not an array, a collection or becauseArgs.
    private static bool IsExpandedParams(InvocationExpressionSyntax link, SemanticModel model) =>
        model.GetOperation(link) is IInvocationOperation { Arguments: [{ ArgumentKind: ArgumentKind.ParamArray }, ..], TargetMethod.Parameters: [{ IsParams: true }, ..] };

    // True for a top invocation that becomes several statements on its own: one of the s_expansions, or a first link whose
    // subject is the WhoseValue of a ContainKey.
    private static bool IsExpandable(InvocationExpressionSyntax top, SemanticModel model)
    {
        InvocationExpressionSyntax first = top;
        for (InvocationExpressionSyntax? previous = GetPrevious(first); previous is not null; previous = GetPrevious(first))
        {
            first = previous;
        }

        return (top.Expression is MemberAccessExpressionSyntax access && s_expansions.ContainsKey(access.Name.Identifier.ValueText))
            || (FluentAssertionsSyntax.GetSubject(first) is { } subject && GetWhoseValueSource(subject, model) is not null);
    }

    // The `x.Should().ContainKey(k)` of `x.Should().ContainKey(k).WhoseValue`, else null.
    private static InvocationExpressionSyntax? GetWhoseValueSource(ExpressionSyntax subject, SemanticModel model) =>
        subject is MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "WhoseValue",
            Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ContainKey" } } source,
        }
            && FluentAssertionsSyntax.GetSubject(source) is not null
            && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(source).Symbol)
                ? source
                : null;

    // A public instance indexer on the type, its bases or its interfaces that takes the key.
    private static bool HasIndexer(ITypeSymbol type, ITypeSymbol? keyType, Compilation compilation) =>
        AllTypes(type).SelectMany(t => t.GetMembers()).OfType<IPropertySymbol>().Any(p =>
            p is { IsIndexer: true, IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null, Parameters.Length: 1 }
            && (keyType is null || compilation.ClassifyConversion(keyType, p.Parameters[0].Type).IsImplicit));

    private static bool HasValues(ITypeSymbol type) =>
        AllTypes(type).SelectMany(t => t.GetMembers("Values")).OfType<IPropertySymbol>().Any(p =>
            p is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null, IsIndexer: false });

    private static IEnumerable<ITypeSymbol> AllTypes(ITypeSymbol type)
    {
        yield return type;
        for (INamedTypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            yield return baseType;
        }

        foreach (INamedTypeSymbol implemented in type.AllInterfaces)
        {
            yield return implemented;
        }
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
