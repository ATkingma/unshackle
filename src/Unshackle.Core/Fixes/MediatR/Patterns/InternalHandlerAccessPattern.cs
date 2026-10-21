using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

// MediatR scans another project's internal handlers by reflection; Mediator's source generator in the host writes code that names them,
// which only compiles when that project grants the host InternalsVisibleTo (CS0122 otherwise, measured on 3.0.2 in TAAK-183/195).
// The attribute goes into one file of the project with the handlers: the first one (by path) that declares an internal handler or message.
public class InternalHandlerAccessPattern : ISolutionPattern, ISolutionFinder
{
    public string Id => "mediatr.internal-handler-access";

    public string Description =>
        "internal handlers scanned by AddMediatR in another project → [assembly: InternalsVisibleTo(\"<that project>\")], so the code Mediator generates there can reach them";

    // Which project scans this one is only known with the solution.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => [];

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("Which project scans these internal handlers is only known with the whole solution; grant it InternalsVisibleTo by hand.");

    public async Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        return root is CompilationUnitSyntax && IsChosenFile(model, cancellationToken) && (await HostsAsync(model, solution, cancellationToken)).Count > 0
            ? [root]
            : [];
    }

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        var unit = (CompilationUnitSyntax)node;
        IReadOnlyList<string> hosts = await HostsAsync(model, solution, cancellationToken);
        SyntaxTrivia endOfLine = unit.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) is { RawKind: not 0 } found ? found : SyntaxFactory.LineFeed;
        List<AttributeListSyntax> attributes =
        [
            .. hosts.Select(h => SyntaxFactory.ParseCompilationUnit(
                    $"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{h}\")]").AttributeLists[0]
                .WithTrailingTrivia(endOfLine)),
        ];

        // After usings and existing assembly attributes, before the first member; the member keeps a blank line in front.
        CompilationUnitSyntax rewritten;
        if (unit.AttributeLists.Count == 0 && unit.Members.FirstOrDefault() is { } first)
        {
            attributes[0] = attributes[0].WithLeadingTrivia(first.GetLeadingTrivia());
            rewritten = unit.ReplaceNode(first, first.WithLeadingTrivia(endOfLine)).WithAttributeLists([.. attributes]);
        }
        else
        {
            rewritten = unit.WithAttributeLists(unit.AttributeLists.AddRange(attributes));
        }

        return PatternResult.Fixed(rewritten);
    }

    // The first file (by path, then position) of this project that declares an internal handler: one attribute per project.
    private static bool IsChosenFile(SemanticModel model, CancellationToken cancellationToken)
    {
        SyntaxReference? first = AddMediatRPattern.Types(model.Compilation.Assembly.GlobalNamespace)
            .Where(t => AddMediatRPattern.IsHiddenHandler(t) || IsHiddenMessage(t))
            .SelectMany(t => t.DeclaringSyntaxReferences)
            .OrderBy(r => r.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(r => r.Span.Start)
            .FirstOrDefault();
        cancellationToken.ThrowIfCancellationRequested();
        return first is not null && first.SyntaxTree.FilePath == model.SyntaxTree.FilePath;
    }

    // An internal, non-generic request or notification (a domain event, say): the host's generator names it even without a handler (CS0122, TAAK-212).
    private static bool IsHiddenMessage(INamedTypeSymbol type) =>
        !type.IsAbstract && type.TypeParameters.Length == 0 && !AddMediatRPattern.IsVisibleOutside(type)
        && type.AllInterfaces.Any(i => i.MetadataName is "IRequest" or "IRequest`1" or "INotification" or "IStreamRequest`1"
            && i.ContainingNamespace.ToDisplayString() is "MediatR" or "Mediator");

    // Assembly names of the other projects whose AddMediatR scans this assembly and that it does not grant InternalsVisibleTo yet, sorted.
    private static async Task<IReadOnlyList<string>> HostsAsync(SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        IAssemblySymbol self = model.Compilation.Assembly;
        // A host gets Mediator's generator itself; a host referencing it lists only the other assemblies (MediatorHosts, TAAK-213).
        if (self.Identity.IsStrongName || (await MediatorHosts.AssemblyNamesAsync(solution, cancellationToken)).Contains(self.Name))
        {
            return [];
        }

        var pattern = new AddMediatRPattern();
        var autofac = new AutofacMediatorModulePattern();
        SortedSet<string> hosts = new(StringComparer.Ordinal);
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp && p.AssemblyName != self.Name))
        {
            foreach (Document document in project.Documents)
            {
                string text = (await document.GetTextAsync(cancellationToken)).ToString();
                if (!(text.Contains("AddMediatR", StringComparison.Ordinal) || text.Contains("RegisterAssemblyTypes", StringComparison.Ordinal))
                    || await document.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root
                    || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel other)
                {
                    continue;
                }

                // An Autofac module registering MediatR by hand gets the generator, which sees the handlers of every referenced project (TAAK-196).
                if ((pattern.Find(root, other).Any(n => AddMediatRPattern.ScannedAssemblies(n, other).Any(a => a.Name == self.Name))
                        || (autofac.Find(root, other).Any() && other.Compilation.SourceModule.ReferencedAssemblySymbols.Any(a => a.Name == self.Name)))
                    && !self.GivesAccessTo(other.Compilation.Assembly))
                {
                    hosts.Add(project.AssemblyName);
                }
            }
        }

        return [.. hosts];
    }
}
