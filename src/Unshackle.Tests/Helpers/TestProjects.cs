using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Unshackle.Tests.Helpers;

// An in-memory solution with several projects, written in one proof file: each project starts with a line
// "// @project Name" or "// @project Name -> Reference1, Reference2" (projects listed earlier), followed by its one document.
public static class TestProjects
{
    public const string Marker = "// @project ";

    private static readonly AdhocWorkspace s_workspace = new();

    public static bool IsMultiProject(string code) => code.StartsWith(Marker, StringComparison.Ordinal);

    public static Solution Create(string code, IEnumerable<MetadataReference> references)
    {
        Solution solution = s_workspace.CurrentSolution;
        Dictionary<string, ProjectId> ids = new(StringComparer.Ordinal);
        foreach ((string Name, IReadOnlyList<string> References, string Header, string Code) section in Parse(code))
        {
            var id = ProjectId.CreateNewId();
            ids[section.Name] = id;
            solution = solution
                .AddProject(id, section.Name, section.Name, LanguageNames.CSharp)
                .WithProjectCompilationOptions(id, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(id, references)
                .AddProjectReferences(id, section.References.Select(r => new ProjectReference(ids[r])))
                .AddDocument(DocumentId.CreateNewId(id), section.Name + ".cs", section.Code, filePath: section.Name + ".cs");
        }

        return solution;
    }

    // The proof text again, in the order of the original sections.
    public static async Task<string> GetTextAsync(Solution solution, string original, CancellationToken cancellationToken)
    {
        List<string> parts = [];
        foreach ((string Name, IReadOnlyList<string> References, string Header, string Code) section in Parse(original))
        {
            Document document = solution.Projects.Single(p => p.Name == section.Name).Documents.Single();
            parts.Add(section.Header + (await document.GetTextAsync(cancellationToken)).ToString());
        }

        return string.Concat(parts);
    }

    // Errors per project, as TestSolution.CompileErrors; the generators run in a project whose code calls one of generatorCalls.
    public static IReadOnlyList<Diagnostic> CompileErrors(
        string code, IEnumerable<MetadataReference> references, IEnumerable<ISourceGenerator>? generators = null, string generatorCall = "AddMediator(")
    {
        Dictionary<string, MetadataReference> built = new(StringComparer.Ordinal);
        List<Diagnostic> errors = [];
        foreach ((string Name, IReadOnlyList<string> References, string Header, string Code) section in Parse(code))
        {
            Compilation compilation = CSharpCompilation.Create(
                section.Name,
                [CSharpSyntaxTree.ParseText(section.Code, path: section.Name + ".cs")],
                [.. references, .. section.References.Select(r => built[r])],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            if (generators is not null && section.Code.Contains(generatorCall, StringComparison.Ordinal))
            {
                CSharpGeneratorDriver.Create([.. generators]).RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
            }

            errors.AddRange(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error || d.Id is "CS8602" or "CS8604"));
            built[section.Name] = compilation.ToMetadataReference();
        }

        return errors;
    }

    private static List<(string Name, IReadOnlyList<string> References, string Header, string Code)> Parse(string code)
    {
        List<(string Name, IReadOnlyList<string> References, string Header, string Code)> sections = [];
        string[] lines = code.Split('\n');
        int start = -1;
        for (int i = 0; i <= lines.Length; i++)
        {
            if (i < lines.Length && !lines[i].StartsWith(Marker, StringComparison.Ordinal))
            {
                continue;
            }

            if (start >= 0)
            {
                string[] header = lines[start][Marker.Length..].Split("->", 2);
                IReadOnlyList<string> references = header.Length == 2
                    ? [.. header[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)]
                    : [];
                sections.Add((header[0].Trim(), references, lines[start] + "\n", string.Join('\n', lines[(start + 1)..i]) + (i < lines.Length ? "\n" : "")));
            }

            start = i;
        }

        return sections;
    }
}
