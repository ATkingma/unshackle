using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 8, E1: every EPPlus proof whose code writes a workbook (a public static Write(string path)) runs through the cell-compare
// harness: before on EPPlus 4.5.3.3 and after on ClosedXML produce the same cells. ProofTests checks the text; this checks the behaviour.
public class EPPlusHarnessProofTests
{
    private static readonly string s_proofsDirectory = Path.Combine(AppContext.BaseDirectory, "Proofs", "EPPlus");

    public static TheoryData<string> HarnessProofs() =>
        [.. Directory.EnumerateFiles(s_proofsDirectory, "*.after.cs", SearchOption.AllDirectories)
            .Where(p => WorkbookProgram.HasEntryPoint(File.ReadAllText(p)))
            .Select(p => Path.GetRelativePath(s_proofsDirectory, p)[..^".after.cs".Length].Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(HarnessProofs))]
    public async Task Proof_AfterWritesSameWorkbook(string proof)
    {
        // Arrange
        string basePath = Path.Combine(s_proofsDirectory, proof);
        string before = await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken);
        string after = await File.ReadAllTextAsync(basePath + ".after.cs", TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldBeEmpty(string.Join(Environment.NewLine, differences));
    }

    // SaveAs(new FileInfo(path)) → SaveAs(path): both overwrite a file that is already there (epplus-package plan).
    [Fact]
    public async Task Proof_SaveAsOverExistingFile_BothOverwrite()
    {
        // Arrange
        string basePath = Path.Combine(s_proofsDirectory, "Package", "using-statement");
        string before = await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken);
        string after = await File.ReadAllTextAsync(basePath + ".after.cs", TestContext.Current.CancellationToken);
        using var directory = new Helpers.TempDirectory();
        string beforePath = Path.Combine(directory.Path, "before.xlsx");
        string afterPath = Path.Combine(directory.Path, "after.xlsx");
        await File.WriteAllTextAsync(beforePath, "not a workbook", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(afterPath, "not a workbook", TestContext.Current.CancellationToken);

        // Act
        WorkbookProgram.Run(before, EPPlusReferences.EPPlus, beforePath);
        WorkbookProgram.Run(after, EPPlusReferences.ClosedXml, afterPath);

        // Assert
        CellCompare.Differences(WorkbookSnapshot.Read(beforePath), WorkbookSnapshot.Read(afterPath)).ShouldBeEmpty();
    }

    // E1 is mandatory for value- and style-touching patterns: a Package proof that writes a workbook must be in the harness.
    [Fact]
    public void HarnessProofs_CoverEveryFixedPackageProof()
    {
        // Arrange
        string[] fixedPackageProofs = [.. Directory.EnumerateFiles(Path.Combine(s_proofsDirectory, "Package"), "*.after.cs")];

        // Act
        IEnumerable<string> withoutHarness = fixedPackageProofs.Where(p => !WorkbookProgram.HasEntryPoint(File.ReadAllText(p)));

        // Assert
        fixedPackageProofs.ShouldNotBeEmpty();
        withoutHarness.ShouldBeEmpty();
    }
}
