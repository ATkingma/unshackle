using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// TAAK-289: an IMapper field, constructor parameter or local that only served Map calls the fix migrates → removed, with its assignments,
/// null checks and the arguments for it at every caller (DI registrations and tests included), and a static factory that only built
/// that mapper. Runs after the call patterns and before <see cref="AddAutoMapperPattern"/>, which then no longer sees an IMapper user.
/// What may go is planned once for the solution by <see cref="MapperCleanup"/>; anything it is not sure of stays as it is.
/// One finding per outermost type (its removals together, so no blank line is left where the first member or statement went).
/// </summary>
public class UnusedMapperPattern : ISolutionPattern, ISolutionFinder
{
    public string Id => "automapper.unused-imapper";

    public string Description =>
        "IMapper field, constructor parameter or local that only served migrated Map calls → removed, with its assignments, null checks and the arguments at every caller";

    // Without the solution the callers are unknown: nothing is removed.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => [];

    public async Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        MapperCleanup cleanup = await MapperCleanup.ForAsync(solution, model, cancellationToken);
        return [.. cleanup.Removals(root, model).Select(r => (SyntaxNode?)r.Node.Ancestors().OfType<TypeDeclarationSyntax>().LastOrDefault() ?? r.Node).Distinct(),
            .. cleanup.KeptLookups(root, model).Select(k => k.Lookup)];
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("Removing an unused IMapper needs the whole solution (its callers); run the fix on the solution.");

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        MapperCleanup cleanup = await MapperCleanup.ForAsync(solution, model, cancellationToken);
        if (node is InvocationExpressionSyntax && cleanup.KeptLookups(node.SyntaxTree.GetRoot(cancellationToken), model).FirstOrDefault(k => k.Lookup == node) is { Reason: string kept, Code: var code })
        {
            return new ManualResult(kept) { Kept = code };
        }

        List<MapperCleanupRemoval> removals = [.. cleanup.Removals(node.SyntaxTree.GetRoot(cancellationToken), model).Where(r => node.Span.Contains(r.Node.Span))];
        string? warning = removals.Select(r => r.Warning).OfType<string>().FirstOrDefault();
        if (removals is [{ Node: var single }] && single == node)
        {
            return Replacement(single, model, cleanup) is { } replacement ? PatternResult.Fixed(replacement) : new FixedResult([], []) { Warning = warning };
        }

        // TAAK-292 (council decision 4): a constructor or class the removals leave empty stays; the finding says so in an Info line.
        List<SyntaxNode> shells = [.. node.DescendantNodesAndSelf().Where(n => n is ConstructorDeclarationSyntax or ClassDeclarationSyntax && !IsEmpty(n))];
        SyntaxNode rewritten = node.TrackNodes(removals.Select(r => r.Node).Concat(shells));
        foreach (MapperCleanupRemoval removal in removals)
        {
            SyntaxNode current = rewritten.GetCurrentNode(removal.Node)!;
            rewritten = Replacement(removal.Node, model, cleanup) is { } replacement
                ? rewritten.ReplaceNode(current, replacement.WithTriviaFrom(current))
                : Remove(rewritten, removal.Node);
        }

        List<string> emptied = [.. shells.Where(s => rewritten.GetCurrentNode(s) is { } now && IsEmpty(now)).Select(Name)];
        string? info = emptied.Count == 0 ? null : $"{string.Join(", ", emptied)} {(emptied.Count == 1 ? "is" : "are")} now empty and kept; removing it is a separate refactoring.";
        return new FixedResult([rewritten], []) { Warning = warning, Info = info };
    }

    // A constructor without parameters, initializer and statements, or a class without members.
    private static bool IsEmpty(SyntaxNode node) => node switch
    {
        ConstructorDeclarationSyntax constructor => constructor.ParameterList.Parameters.Count == 0 && constructor.Initializer is null
            && constructor.ExpressionBody is null && constructor.Body is { Statements.Count: 0 },
        ClassDeclarationSyntax type => type.Members.Count == 0,
        _ => false,
    };

    private static string Name(SyntaxNode node) => node switch
    {
        ConstructorDeclarationSyntax constructor => $"the constructor {constructor.Identifier.ValueText}()",
        ClassDeclarationSyntax type => $"the class {type.Identifier.ValueText}",
        _ => node.Kind().ToString(),
    };

    // The trimmed node, or null when the whole node goes. original is in the tree of model.
    private static SyntaxNode? Replacement(SyntaxNode original, SemanticModel model, MapperCleanup cleanup) => original switch
    {
        FieldDeclarationSyntax field when field.Declaration.Variables.Where(v => !cleanup.IsRemoved(v, model)).ToList() is { Count: > 0 } kept
            => field.WithDeclaration(field.Declaration.WithVariables(SyntaxFactory.SeparatedList(kept))),
        ParameterListSyntax parameters => parameters.RemoveNodes(parameters.Parameters.Where(p => cleanup.IsRemoved(p, model)), SyntaxRemoveOptions.KeepNoTrivia),
        ArgumentListSyntax arguments => arguments.RemoveNodes(cleanup.RemovedArguments(arguments, model), SyntaxRemoveOptions.KeepNoTrivia),
        _ => null,
    };

    // Removes a member or statement (tracked in root); when it was the first of its list, the next one loses its leading blank lines.
    private static SyntaxNode Remove(SyntaxNode root, SyntaxNode original)
    {
        SyntaxNode node = root.GetCurrentNode(original)!;
        List<SyntaxNode> siblings = [.. node.Parent!.ChildNodes().Where(n => n is MemberDeclarationSyntax or StatementSyntax)];
        if (siblings.IndexOf(node) == 0 && siblings.Count > 1)
        {
            SyntaxNode next = siblings[1];
            SyntaxTriviaList leading = next.GetLeadingTrivia();
            int blank = 0;
            while (blank < leading.Count && (leading[blank].IsKind(SyntaxKind.EndOfLineTrivia) || (leading[blank].IsKind(SyntaxKind.WhitespaceTrivia)
                && blank + 1 < leading.Count && leading[blank + 1].IsKind(SyntaxKind.EndOfLineTrivia))))
            {
                blank++;
            }

            root = root.ReplaceNode(next, next.WithLeadingTrivia(leading.Skip(blank)));
            node = root.GetCurrentNode(original)!;
        }

        return root.RemoveNode(node, SyntaxRemoveOptions.KeepUnbalancedDirectives)!;
    }
}
