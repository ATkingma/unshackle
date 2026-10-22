using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// What an EPPlus integer worksheet index means in one project (council round 11, E10/E11, superseding the 4.x part of E3): the first
// index (0 or 1), or the Manual result that says why it is not provable.
public class WorksheetIndexBase
{
    public const string Key = "IsWorksheets1Based";

    // Said with every Fixed index (E11): the override is only looked for in these places.
    public const string Checked = $"{Key} was looked for in the solution's config files and code only.";

    public int? First { get; init; }

    public PatternResult? Blocked { get; init; }

    // Without the solution (a plain Rewrite): nothing about the project is known.
    public static WorksheetIndexBase Unknown(string where) => new()
    {
        Blocked = PatternResult.Manual(EPPlusReason.WorksheetIndexBaseNotProvable,
            $"Worksheets[...] ({where}): the project's target framework, packability and configuration are unknown without the solution."),
    };

    public static async Task<WorksheetIndexBase> ForAsync(
        SemanticModel model, Solution solution, IProjectProperties? properties, IConfigKeySearch? configSearch, string where, CancellationToken cancellationToken)
    {
        // E11: the override anywhere in code or config makes every integer index Manual, also on 5+ (the value is not read).
        string? overrideAt = await InCodeAsync(solution, cancellationToken) ?? InConfig(solution, configSearch);
        if (overrideAt is not null)
        {
            return new WorksheetIndexBase
            {
                Blocked = PatternResult.Manual(EPPlusReason.WorksheetIndexConfigured,
                    $"Worksheets[...] ({where}): {Key} is set at {overrideAt}, which can change the index base; remove it to make the index fixable on the next run. {Checked}"),
            };
        }

        if (configSearch is null || Path.GetDirectoryName(solution.FilePath) is not { Length: > 0 })
        {
            return new WorksheetIndexBase
            {
                Blocked = PatternResult.Manual(EPPlusReason.WorksheetIndexConfigured,
                    $"Worksheets[...] ({where}): the config files could not be searched for {Key}, so the index base is not provable."),
            };
        }

        Version? version = EPPlusSyntax.Version(model);
        if (version is null)
        {
            return new WorksheetIndexBase
            {
                Blocked = PatternResult.Manual(EPPlusReason.UnknownEPPlusVersion,
                    $"Worksheets[...] ({where}): the referenced EPPlus version is unknown, so its index base is (4.x by target framework, 5+ 0-based)."),
            };
        }

        if (version.Major >= 5)
        {
            return new WorksheetIndexBase { First = 0 };
        }

        // E10: EPPlus 4.x loads its net40 build (1-based) on .NET Framework and its netstandard build (0-based) on .NET Core / .NET 5+.
        Project? project = solution.GetDocument(model.SyntaxTree)?.Project;
        if (project is null)
        {
            return Unknown(where);
        }

        string family = Family(solution, project);
        bool packable = project.FilePath is null || properties is null || properties.IsPackable(project.FilePath);
        string why = family switch
        {
            _ when packable => "the project is packable (or its packability is unknown), so callers on other frameworks may load it",
            "net4x" or "netcore" when Dependents(solution, project).Select(d => Family(solution, d)).FirstOrDefault(f => f != family) is string other =>
                $"a project that depends on it targets {other}, while it targets {family}",
            "net4x" or "netcore" => "",
            "netstandard" => "it targets netstandard, so the framework that loads it decides",
            "mixed" => "its target frameworks are of different families (.NET Framework and .NET Core / .NET 5+)",
            _ => "its target framework is unknown",
        };
        return why.Length == 0
            ? new WorksheetIndexBase { First = family == "net4x" ? 1 : 0 }
            : new WorksheetIndexBase
            {
                Blocked = PatternResult.Manual(EPPlusReason.WorksheetIndexBaseNotProvable,
                    $"Worksheets[...] ({where}): EPPlus {version.Major}.x is 1-based on .NET Framework and 0-based on .NET Core / .NET 5+ (target framework family: {family}); {why}. Use the sheet name or rewrite by hand."),
            };
    }

    // net4x, netcore, netstandard, mixed or unknown, over every target framework of the project (one Roslyn project per TFM).
    private static string Family(Solution solution, Project project)
    {
        HashSet<string> families = [.. solution.Projects.Where(p => p.FilePath is not null && p.FilePath == project.FilePath || p.Id == project.Id).Select(FamilyOf)];
        return families.Count == 1 ? families.Single() : "mixed";
    }

    private static string FamilyOf(Project project)
    {
        HashSet<string> symbols = [.. project.ParseOptions?.PreprocessorSymbolNames ?? []];
        return symbols.Contains("NETFRAMEWORK") ? "net4x"
            : symbols.Contains("NETCOREAPP") ? "netcore"
            : symbols.Contains("NETSTANDARD") ? "netstandard"
            : "unknown";
    }

    private static IEnumerable<Project> Dependents(Solution solution, Project project) =>
        solution.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(project.Id).Select(solution.GetProject).OfType<Project>();

    // E11 in code: a string literal or identifier IsWorksheets1Based in any document of the solution.
    private static async Task<string?> InCodeAsync(Solution solution, CancellationToken cancellationToken)
    {
        foreach (Document document in solution.Projects.SelectMany(p => p.Documents))
        {
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode? hit = root?.DescendantNodes().FirstOrDefault(n =>
                n is IdentifierNameSyntax { Identifier.ValueText: Key }
                || n is LiteralExpressionSyntax literal && literal.Token.ValueText.Contains(Key, StringComparison.Ordinal));
            if (hit is not null)
            {
                return $"{document.FilePath ?? document.Name}:{hit.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            }
        }

        return null;
    }

    private static string? InConfig(Solution solution, IConfigKeySearch? configSearch) =>
        configSearch is not null && Path.GetDirectoryName(solution.FilePath) is { Length: > 0 } directory
            ? configSearch.Find(directory, Key) is [var first, ..] ? first : null
            : null;
}
