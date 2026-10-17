using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Unshackle.Core.Fixes.FluentAssertions;

// Takes the decision per custom assertions class once per run (the first answer is kept, so every file agrees): the class
// has a Shouldly form, it is only used through `x.Should().Method(...)` in forms that convert, and nothing else mentions it.
public class CustomAssertionPlans
{
    // Per solution snapshot: every file of one run is fixed against the same baseline, a later run gets a new one.
    private readonly ConditionalWeakTable<Solution, ConcurrentDictionary<string, Task<CustomAssertionPlan>>> _plans = [];

    public Task<CustomAssertionPlan> GetAsync(INamedTypeSymbol assertions, Solution solution, CancellationToken cancellationToken)
    {
        INamedTypeSymbol definition = assertions.OriginalDefinition;
        return _plans.GetOrCreateValue(solution).GetOrAdd(definition.ToDisplayString(), _ => ComputeAsync(definition, solution, cancellationToken));
    }

    // The reason a `x.Should()` use cannot convert, or null. The rewrite runs the same check on the same code.
    public static string? CheckUsage(CustomAssertionChain chain, SemanticModel model, CustomAssertionPlan plan)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(chain.Top, model))
        {
            return FluentAssertionsSyntax.InsideAssertionScopeReason;
        }

        if (FluentAssertionsSyntax.IsInsideExpectedFailure(chain.Top, model))
        {
            return FluentAssertionsSyntax.ExpectedFailureReason;
        }

        bool anyCustom = false;
        foreach (InvocationExpressionSyntax link in chain.Links)
        {
            if (model.GetSymbolInfo(link).Symbol is not IMethodSymbol method)
            {
                return "a call in the chain does not bind";
            }

            if (FluentAssertionsSyntax.IsFluentAssertions(method))
            {
                continue;
            }

            anyCustom = true;
            if (method.ContainingType.Name != plan.ClassName || !plan.Shape.MethodNames.Contains(method.Name))
            {
                return $"{method.ContainingType.Name}.{method.Name} is not an assertion method of {plan.ClassName}";
            }

            if (!FluentAssertionsSyntax.TryGetArguments(link, model, out _, out _))
            {
                return $"{method.Name}(...) with named arguments, or becauseArgs whose because is no constant format string";
            }
        }

        if (!anyCustom)
        {
            return null;
        }

        if (chain.HasAnd)
        {
            return chain.Statement is { Parent: BlockSyntax or SwitchSectionSyntax }
                ? null
                : "an .And chain that is not a statement directly in a block cannot be split into statements";
        }

        return FluentAssertionsSyntax.IsResultDiscarded(chain.Top, model)
            ? null
            : "the result of the assertion is used (.And/.Which or a value); the Shouldly form returns nothing";
    }

    private static async Task<CustomAssertionPlan> ComputeAsync(INamedTypeSymbol assertions, Solution solution, CancellationToken cancellationToken)
    {
        CustomAssertionPlan Fail(string reason, CustomAssertionShape? shape = null) =>
            new() { ClassName = assertions.Name, Shape = shape ?? CustomAssertionShape.Fail(reason), Reason = reason };

        if (assertions.DeclaringSyntaxReferences is not [{ } reference]
            || await reference.GetSyntaxAsync(cancellationToken) is not ClassDeclarationSyntax declaration
            || solution.GetDocument(declaration.SyntaxTree) is not { } document)
        {
            return Fail("the class is declared in a referenced assembly, in several parts, or outside the solution");
        }

        SemanticModel model = (await document.GetSemanticModelAsync(cancellationToken))!;
        CustomAssertionShape shape = CustomAssertionAnalyzer.Analyze(declaration, model);
        if (shape.Reason is not null)
        {
            return Fail(shape.Reason, shape);
        }

        var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(declaration, cancellationToken)!;
        string? usage = await CheckMentionsAsync(symbol, declaration, shape, solution, cancellationToken)
            ?? CheckShouldly(model, shape)
            ?? await CheckInactiveCodeAsync(shape, solution, cancellationToken);
        return usage is null
            ? new CustomAssertionPlan { ClassName = assertions.Name, Shape = shape }
            : Fail(usage, shape);
    }

    // A generated Should{Name} next to Shouldly's own extension of that name would be ambiguous or hide it.
    private static string? CheckShouldly(SemanticModel model, CustomAssertionShape shape)
    {
        INamespaceSymbol? shouldly = model.Compilation.GlobalNamespace.GetNamespaceMembers().FirstOrDefault(n => n.Name == "Shouldly");
        if (shouldly is null)
        {
            return null;
        }

        HashSet<string> taken = [.. shouldly.GetTypeMembers().SelectMany(t => t.GetMembers()).OfType<IMethodSymbol>().Select(m => m.Name)];
        return shape.MethodNames.FirstOrDefault(n => taken.Contains("Should" + n)) is { } clash
            ? $"Should{clash} would clash with a Shouldly method of that name"
            : null;
    }

    // A call in an inactive #if branch is invisible to SymbolFinder and would break once the class is gone.
    private static async Task<string?> CheckInactiveCodeAsync(CustomAssertionShape shape, Solution solution, CancellationToken cancellationToken)
    {
        foreach (Document document in solution.Projects.SelectMany(p => p.Documents))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            if (root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && shape.MethodNames.Any(n => t.ToString().Contains(n, StringComparison.Ordinal))))
            {
                return $"{Path.GetFileName(document.FilePath)} may call it in an inactive #if branch";
            }
        }

        return null;
    }

    // Every mention of the class is its own declaration or the Should() extension that creates it, and every use of that
    // extension is a call that converts. Null when so, else why not.
    private static async Task<string?> CheckMentionsAsync(
        INamedTypeSymbol symbol, ClassDeclarationSyntax declaration, CustomAssertionShape shape, Solution solution, CancellationToken cancellationToken)
    {
        var candidates = new Dictionary<MethodDeclarationSyntax, Document>();
        var others = new List<(ReferenceLocation Location, MethodDeclarationSyntax? Method)>();
        foreach (ReferencedSymbol referenced in await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken))
        {
            foreach (ReferenceLocation location in referenced.Locations.Where(l => !l.IsImplicit))
            {
                SyntaxTree tree = location.Location.SourceTree!;
                if (tree == declaration.SyntaxTree && declaration.Span.Contains(location.Location.SourceSpan))
                {
                    continue;
                }

                SyntaxNode node = (await tree.GetRootAsync(cancellationToken)).FindNode(location.Location.SourceSpan);
                MethodDeclarationSyntax? method = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (method is not null && method.ReturnType.Span.Contains(location.Location.SourceSpan))
                {
                    candidates[method] = location.Document;
                }
                else
                {
                    others.Add((location, method));
                }
            }
        }

        if (others.FirstOrDefault(o => o.Method is null || !candidates.ContainsKey(o.Method)) is { Location.Location: { } stray })
        {
            return $"{symbol.Name} is used by name at {Path.GetFileName(stray.SourceTree!.FilePath)}:{stray.GetLineSpan().StartLinePosition.Line + 1}, not only through Should()";
        }

        if (candidates.Count == 0)
        {
            return "no Should() extension that creates it was found in the solution";
        }

        string subject = ((INamedTypeSymbol)symbol.BaseType!).TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
        foreach ((MethodDeclarationSyntax method, Document document) in candidates)
        {
            SemanticModel model = (await document.GetSemanticModelAsync(cancellationToken))!;
            ExpressionSyntax? body = method.ExpressionBody?.Expression
                ?? (method.Body?.Statements is [ReturnStatementSyntax { Expression: { } returned }] ? returned : null);
            if (model.GetDeclaredSymbol(method, cancellationToken) is not IMethodSymbol { IsStatic: true, IsExtensionMethod: true, Name: "Should", Parameters: [{ } parameter] } should
                || parameter.Type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString() != subject
                || body is not (ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)
                || (body as BaseObjectCreationExpressionSyntax)?.ArgumentList is not { Arguments: [{ NameColon: null, Expression: IdentifierNameSyntax argument }] }
                || argument.Identifier.ValueText != parameter.Name)
            {
                return "it is created by a method other than a plain `static C Should(this T instance) => new(instance)`";
            }

            foreach (ReferencedSymbol referenced in await SymbolFinder.FindReferencesAsync(should, solution, cancellationToken))
            {
                foreach (ReferenceLocation location in referenced.Locations)
                {
                    SemanticModel useModel = (await location.Document.GetSemanticModelAsync(cancellationToken))!;
                    SyntaxNode name = (await location.Location.SourceTree!.GetRootAsync(cancellationToken)).FindNode(location.Location.SourceSpan);
                    if (name.Parent is not MemberAccessExpressionSyntax access || access.Name != name || access.Parent is not InvocationExpressionSyntax call
                        || call.Expression != access)
                    {
                        return "Should() is used other than as x.Should()";
                    }

                    var plan = new CustomAssertionPlan { ClassName = symbol.Name, Shape = shape };
                    string? reason = CustomAssertionChain.TryParse(call, useModel, out CustomAssertionChain? chain) ?? CheckUsage(chain!, useModel, plan);
                    if (reason is not null)
                    {
                        Location place = location.Location;
                        return $"{reason} (at {Path.GetFileName(place.SourceTree!.FilePath)}:{place.GetLineSpan().StartLinePosition.Line + 1})";
                    }
                }
            }
        }

        return null;
    }
}
