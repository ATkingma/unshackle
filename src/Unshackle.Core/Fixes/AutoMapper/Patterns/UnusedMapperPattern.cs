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
        return [.. cleanup.Removals(root, model).Select(r => r.Node)];
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("Removing an unused IMapper needs the whole solution (its callers); run the fix on the solution.");

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        MapperCleanup cleanup = await MapperCleanup.ForAsync(solution, model, cancellationToken);
        string? warning = cleanup.Removals(node.SyntaxTree.GetRoot(cancellationToken), model).FirstOrDefault(r => r.Node == node)?.Warning;
        return node switch
        {
            FieldDeclarationSyntax field when field.Declaration.Variables.Where(v => !cleanup.IsRemoved(v, model)).ToList() is { Count: > 0 } kept
                => PatternResult.Fixed(field.WithDeclaration(field.Declaration.WithVariables(SyntaxFactory.SeparatedList(kept)))),
            ParameterListSyntax parameters => PatternResult.Fixed(parameters.RemoveNodes(parameters.Parameters.Where(p => cleanup.IsRemoved(p, model)), SyntaxRemoveOptions.KeepNoTrivia)!),
            ArgumentListSyntax arguments => PatternResult.Fixed(arguments.RemoveNodes(cleanup.RemovedArguments(arguments, model), SyntaxRemoveOptions.KeepNoTrivia)!),
            _ => new FixedResult([], []) { Warning = warning },
        };
    }
}
