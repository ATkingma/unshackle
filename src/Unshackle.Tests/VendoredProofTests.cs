using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests;

// Proofs/MediatR/_Combined/Vendored: code on Ardalis.SharedKernel, a package that builds on MediatR, through the real MediatRFix.
// Expected: <case>.after.cs (exact text of the user's file) plus the vendored source of the package, and together they compile
// against Mediator and the packages Ardalis.SharedKernel depended on, without the package itself (TAAK-194).
public class VendoredProofTests
{
    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "MediatR", CombinedProofTests.FolderName, "Vendored");

    public static TheoryData<string> Cases() =>
        [.. Directory.EnumerateFiles(s_directory, "*.before.cs")
            .Select(p => Path.GetFileName(p)[..^".before.cs".Length])
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Vendored_Before_BecomesAfterPlusSource(string name)
    {
        // Arrange
        string basePath = Path.Combine(s_directory, name);
        string before = Normalize(await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken));
        string expected = Normalize(await File.ReadAllTextAsync(basePath + ".after.cs", TestContext.Current.CancellationToken));
        TestSolution.CompileErrors(before, TestReferences.MediatRSharedKernel).ShouldBeEmpty("before must compile against MediatR and Ardalis.SharedKernel");
        var fixer = new Fixer([new MediatRFix()]);
        Solution solution = TestSolution.Create(before, TestReferences.MediatRSharedKernel);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        List<Document> documents = [.. report.Solution.Projects.Single().Documents];
        Document user = documents.Single(d => d.Name == TestSolution.FileName);
        string actual = (await user.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        actual.ShouldBe(expected);
        List<Document> vendored = [.. documents.Where(d => d != user)];
        vendored.Select(d => d.FilePath!.Replace('\\', '/')).ShouldBe(
            ArdalisSharedKernel.Package.Versions.Single(v => v.Version == "1.4.0").Files.Select(f => "Ardalis.SharedKernel/" + f.Name), ignoreOrder: true);
        report.ProjectFileChanges.Where(c => c.Creates).Select(c => c.FilePath.Replace('\\', '/'))
            .ShouldBe([.. vendored.Select(d => d.FilePath!.Replace('\\', '/')), "Ardalis.SharedKernel/LICENSE.txt"], ignoreOrder: true);
        List<SyntaxTree> trees = [];
        foreach (Document document in documents)
        {
            trees.Add(CSharpSyntaxTree.ParseText((await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString(), path: document.FilePath!, cancellationToken: TestContext.Current.CancellationToken));
        }

        Compilation compilation = CSharpCompilation.Create("Check", trees, TestReferences.MediatorSharedKernel, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        CSharpGeneratorDriver.Create([.. TestReferences.MediatorGenerators]).RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _, TestContext.Current.CancellationToken);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity >= DiagnosticSeverity.Warning && d.Id is not ("CS1701" or "CS1702"))
            .ShouldBeEmpty("after plus the vendored source must compile against Mediator without Ardalis.SharedKernel, warnings included (the binding-redirect notes the SDK also suppresses aside)");
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");
}
