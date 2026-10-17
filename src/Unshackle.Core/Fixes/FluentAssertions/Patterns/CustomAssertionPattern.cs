using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// A method of the user (or another library) on a FluentAssertions assertions type, such as `result.Should().BeFailure()`.
// A `class XAssertions : ReferenceTypeAssertions<X, XAssertions>` whose methods are only Execute.Assertion.ForCondition(...)
// .FailWith(...) becomes Shouldly-style extension methods (TAAK-275): the class, its `Should()` extension and every call
// convert together, or none does (CustomAssertionPlans decides once per class). Everything else is Manual; the reason is
// the same for every call of one method, so the report and console group on it, one fix per extension instead of per call.
public class CustomAssertionPattern : ISolutionFinder, ISolutionPattern
{
    public const string PatternId = "fa.custom-assertion";

    private readonly CustomAssertionPlans _plans = new();

    // Literal for check-pattern-docs.sh.
    public string Id => "fa.custom-assertion";

    public string Description => "x.Should().BeCancelled() on an own ReferenceTypeAssertions class → x.ShouldBeCancelled(); other custom assertions → Manual, grouped";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => FindCalls(root, model);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => PatternResult.Manual(ManualReason((InvocationExpressionSyntax)node, model, null));

    public async Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        var nodes = new List<SyntaxNode>();
        foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration, cancellationToken) is { } symbol && IsAssertionsClass(symbol)
                && (await _plans.GetAsync(symbol, solution, cancellationToken)).Reason is null)
            {
                nodes.Add(declaration);
            }
        }

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "Should"))
        {
            if (model.GetDeclaredSymbol(method, cancellationToken) is { IsExtensionMethod: true, ReturnType: INamedTypeSymbol returned } && IsAssertionsClass(returned)
                && (await _plans.GetAsync(returned, solution, cancellationToken)).Reason is null)
            {
                // The holder class goes too when the Should() extension is all it has.
                nodes.Add(method.Parent is ClassDeclarationSyntax { Members.Count: 1 } holder ? holder : method);
            }
        }

        foreach (InvocationExpressionSyntax call in FindCalls(root, model))
        {
            INamedTypeSymbol? assertions = AssertionsClassOf(call, model);
            SyntaxNode node = call;
            if (assertions is not null && (await _plans.GetAsync(assertions, solution, cancellationToken)).Reason is null
                && ParseChain(call, model) is { } chain)
            {
                node = chain.HasAnd ? chain.Statement! : chain.Top;
            }

            if (!nodes.Contains(node))
            {
                nodes.Add(node);
            }
        }

        return nodes;
    }

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case ClassDeclarationSyntax declaration when model.GetDeclaredSymbol(declaration, cancellationToken) is { } symbol && IsAssertionsClass(symbol):
                CustomAssertionPlan plan = await _plans.GetAsync(symbol, solution, cancellationToken);
                string text = plan.Shape.Render(Indentation(declaration), EndOfLine(declaration));
                return PatternResult.Fixed(SyntaxFactory.ParseMemberDeclaration(text)!, "Shouldly");
            case MemberDeclarationSyntax:
                return PatternResult.Fixed([]);
            case InvocationExpressionSyntax call when AssertionsClassOf(call, model) is not { } assertions:
                return PatternResult.Manual(ManualReason(call, model, null));
            case InvocationExpressionSyntax call when (await _plans.GetAsync(AssertionsClassOf(call, model)!, solution, cancellationToken)).Reason is { } reason:
                return PatternResult.Manual(ManualReason(call, model, reason));
            default:
                InvocationExpressionSyntax top = node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax statementTop } ? statementTop : (InvocationExpressionSyntax)node;
                return ParseChain(top, model) is { } chain
                    ? RewriteChain(chain, model)
                    : PatternResult.Manual(ManualReason(top, model, "the call is not x.Should().Method(...)"));
        }
    }

    // The Manual findings of this pattern, one group per custom assertion (the reason names it), in order of first call.
    public static IEnumerable<IGrouping<string, Finding>> Group(IEnumerable<Finding> findings) =>
        findings
            .Where(f => f.PatternId == PatternId && f.Result is ManualResult)
            .GroupBy(f => ((ManualResult)f.Result).Reason, StringComparer.Ordinal);

    // One statement per link: an own assertion becomes `x.ShouldFoo(args, because)`, a FluentAssertions link stays
    // `x.Should().Bar(args)` for the assertion patterns after this one. x is evaluated once, as FluentAssertions does.
    private static PatternResult RewriteChain(CustomAssertionChain chain, SemanticModel model)
    {
        var statements = new List<SyntaxNode>();
        ExpressionSyntax receiver = chain.Subject.WithoutTrivia();
        if (chain.HasAnd && !FluentAssertionsSyntax.IsSafeToEvaluateTwice(chain.Subject, model))
        {
            (StatementSyntax declaration, IdentifierNameSyntax local) = FluentAssertionsSyntax.Hoist(chain.Subject, model);
            statements.Add(declaration);
            receiver = local;
        }

        var calls = new List<ExpressionSyntax>();
        foreach (InvocationExpressionSyntax link in chain.Links)
        {
            string name = ((MemberAccessExpressionSyntax)link.Expression).Name.Identifier.ValueText;
            if (FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(link).Symbol))
            {
                calls.Add(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, FluentAssertionsSyntax.Invoke(receiver, "Should"), SyntaxFactory.IdentifierName(name)),
                    link.ArgumentList.WithoutTrivia()));
                continue;
            }

            FluentAssertionsSyntax.TryGetArguments(link, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because);
            calls.Add(FluentAssertionsSyntax.Invoke(receiver, "Should" + name, [.. values.Select(v => v.WithoutTrivia()), because?.WithoutTrivia()]));
        }

        if (!chain.HasAnd)
        {
            return PatternResult.Fixed(calls[0]);
        }

        statements.AddRange(calls.Select(c => SyntaxFactory.ExpressionStatement(c)));
        return PatternResult.Fixed(statements);
    }

    private static CustomAssertionChain? ParseChain(InvocationExpressionSyntax call, SemanticModel model)
    {
        // Walk down `.And.` links to the first one, whose receiver is x.Should().
        InvocationExpressionSyntax first = call;
        while (first.Expression is MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "And", Expression: InvocationExpressionSyntax previous } })
        {
            first = previous;
        }

        return first.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax should }
            && CustomAssertionChain.TryParse(should, model, out CustomAssertionChain? chain) is null
                ? chain
                : null;
    }

    // The own ReferenceTypeAssertions subclass the call is a method of, else null (an extension on StringAssertions, a library).
    private static INamedTypeSymbol? AssertionsClassOf(InvocationExpressionSyntax call, SemanticModel model) =>
        model.GetSymbolInfo(call).Symbol is IMethodSymbol { ReducedFrom: null, ContainingType: { } type } && IsAssertionsClass(type) ? type : null;

    private static bool IsAssertionsClass(INamedTypeSymbol type) =>
        type.BaseType is { Name: "ReferenceTypeAssertions" } baseType && FluentAssertionsSyntax.IsFluentAssertions(baseType)
        && type.DeclaringSyntaxReferences.Length > 0;

    private static string ManualReason(InvocationExpressionSyntax call, SemanticModel model, string? reason)
    {
        var method = (IMethodSymbol)model.GetSymbolInfo(call).Symbol!;
        IMethodSymbol definition = (method.ReducedFrom ?? method).OriginalDefinition;
        string where = definition.Locations.FirstOrDefault(l => l.IsInSource) is { } location
            ? Path.GetFileName(location.SourceTree!.FilePath)
            : definition.ContainingAssembly.Name;
        return $"Custom assertion `{definition.ContainingType.Name}.{definition.Name}` ({where}): "
            + (reason is null ? string.Empty : $"no automatic Shouldly form because {reason}; ")
            + "rewrite this FluentAssertions extension to Shouldly once; its calls can stay.";
    }

    private static IEnumerable<InvocationExpressionSyntax> FindCalls(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax && IsCustomAssertion(model.GetSymbolInfo(i).Symbol as IMethodSymbol));

    private static string Indentation(SyntaxNode node) =>
        string.Concat(node.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Select(t => t.ToString()));

    private static string EndOfLine(SyntaxNode node) =>
        node.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } eol ? eol : "\n";

    private static bool IsCustomAssertion(IMethodSymbol? method)
    {
        if (method is null || FluentAssertionsSyntax.IsFluentAssertions(method))
        {
            return false;
        }

        for (ITypeSymbol? type = method.ReducedFrom is null ? method.ContainingType : method.ReceiverType; type is not null; type = type.BaseType)
        {
            if (FluentAssertionsSyntax.IsFluentAssertions(type) && type.Name.EndsWith("Assertions", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
