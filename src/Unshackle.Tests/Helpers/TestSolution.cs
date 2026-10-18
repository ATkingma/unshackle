using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Unshackle.Tests.Helpers;

// An in-memory solution with one project and one document "Test.cs".
public static class TestSolution
{
    public const string FileName = "Test.cs";

    // A solution with an id of its own: MapOwners caches its plan per SolutionId, so tests that run in parallel must never share one (TAAK-284).
    public static Solution NewEmpty() => new AdhocWorkspace().CurrentSolution;

    public static Solution Create(string code, IEnumerable<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        return NewEmpty()
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(projectId, references)
            .AddDocument(DocumentId.CreateNewId(projectId), FileName, code, filePath: FileName);
    }

    public static async Task<string> GetTextAsync(Solution solution, CancellationToken cancellationToken)
    {
        Document document = solution.Projects.Single().Documents.Single();
        return (await document.GetTextAsync(cancellationToken)).ToString();
    }

    // Errors, plus the nullable warnings a rewrite must not add: projects with warnings-as-errors would not build.
    // Source generators run first, like in a real build.
    public static IReadOnlyList<Diagnostic> CompileErrors(
        string code, IEnumerable<MetadataReference> references, IEnumerable<ISourceGenerator>? generators = null)
    {
        Compilation compilation = CSharpCompilation.Create(
            "Check",
            [CSharpSyntaxTree.ParseText(code)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CSharpGeneratorDriver.Create([.. generators ?? []]).RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
        return [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error || d.Id is "CS8602" or "CS8604")];
    }
}
