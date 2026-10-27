using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 10, E7: every migrated package warns about SixLabors.Fonts 2.x; a project that already resolves 2.x is Manual.
public class FontsLicenceTests
{
    private const string Code = """
        using System.IO;
        using OfficeOpenXml;

        public static class Report
        {
            public static void Write(string path)
            {
                using var package = new ExcelPackage();
                package.Workbook.Worksheets.Add("Report");
                package.SaveAs(new FileInfo(path));
            }
        }
        """;

    [Fact]
    public void Rewrite_NoFontsReference_FixedWithLicenceWarning()
    {
        // Arrange, Act
        PatternResult result = Rewrite([]);

        // Assert
        result.ShouldBeOfType<FixedResult>().Warning.ShouldNotBeNull().ShouldContain("Six Labors Split License");
    }

    [Theory]
    [InlineData("1.0.0.0", false)]
    [InlineData("2.1.3.0", true)]
    public void Rewrite_ResolvedFontsVersion_ManualFromTwo(string version, bool manual)
    {
        // Arrange
        MetadataReference fonts = Fonts(version);

        // Act
        PatternResult result = Rewrite([fonts]);

        // Assert
        if (manual)
        {
            result.ShouldBeOfType<ManualResult>().Code.ShouldBe("LicensedDependency");
        }
        else
        {
            result.ShouldBeOfType<FixedResult>();
        }
    }

    [Theory]
    [InlineData("1.0.0.0", 0)]
    [InlineData("2.1.3.0", 1)]
    public async Task ProjectBlockers_ResolvedFontsVersion_LicensedDependencyFromTwo(string version, int expected)
    {
        // Arrange
        Project project = Helpers.TestSolution.Create(Code, [.. EPPlusReferences.EPPlus, Fonts(version)]).Projects.Single();

        // Act
        IReadOnlyList<ManualResult> blockers = await new Unshackle.Core.Fixes.EPPlus.EPPlusFix().ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

        // Assert
        blockers.Count.ShouldBe(expected);
        blockers.ShouldAllBe(b => b.Code == nameof(ProjectBlockingReason.LicensedDependency));
    }

    // TAAK-319, E7: the pin needs current assets of a project that declares EPPlus and nothing requiring Fonts 2.0.
    [Theory]
    [InlineData(AssetsState.Current, "EPPlus", "[1.0.0, )", 1)]
    [InlineData(AssetsState.Current, "EPPlus", "[2.0.0, )", 0)]
    [InlineData(AssetsState.Current, "Other", "[1.0.0, )", 0)]
    [InlineData(AssetsState.Stale, "EPPlus", "[1.0.0, )", 0)]
    [InlineData(AssetsState.Missing, "EPPlus", "[1.0.0, )", 0)]
    public async Task EditsFor_Assets_PinFontsOnlyWhenSafe(AssetsState state, string declared, string fontsRange, int expected)
    {
        // Arrange
        PackageAssets assets = Assets(state, "1.0.1", new PackageRequirement { Package = declared, RequiredBy = "project", Range = "4.5.3.3" },
            new PackageRequirement { Package = "SixLabors.Fonts", RequiredBy = "ClosedXML/0.105.1", Range = fontsRange });

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await Fix(assets).EditsForAsync(ProjectAt("/r/P.csproj"), TestContext.Current.CancellationToken);

        // Assert
        edits.Count.ShouldBe(expected);
        edits.OfType<PackageAddEdit>().ShouldAllBe(e => e.Name == "SixLabors.Fonts" && e.Version == "1.0.0");
    }

    [Theory]
    [InlineData(AssetsState.Current, "2.1.0", 1)]
    [InlineData(AssetsState.Current, "1.0.1", 0)]
    [InlineData(AssetsState.Missing, "2.1.0", 0)]
    public async Task ProjectBlockers_Assets_LicensedDependencyFromTwo(AssetsState state, string fonts, int expected)
    {
        // Arrange
        PackageAssets assets = Assets(state, fonts);

        // Act
        IReadOnlyList<ManualResult> blockers = await Fix(assets).ProjectBlockersAsync(ProjectAt("/r/P.csproj"), TestContext.Current.CancellationToken);

        // Assert
        blockers.Count.ShouldBe(expected);
        blockers.ShouldAllBe(b => b.Code == nameof(ProjectBlockingReason.LicensedDependency));
    }

    private static Unshackle.Core.Fixes.EPPlus.EPPlusFix Fix(PackageAssets assets)
    {
        IPackageAssetsReader reader = Substitute.For<IPackageAssetsReader>();
        reader.Read(Arg.Any<string>()).Returns(assets);
        return new Unshackle.Core.Fixes.EPPlus.EPPlusFix(null, null, reader);
    }

    private static PackageAssets Assets(AssetsState state, string fonts, params PackageRequirement[] requirements) =>
        new() { State = state, Resolved = new Dictionary<string, string> { ["SixLabors.Fonts"] = fonts }, Requirements = requirements };

    private static Project ProjectAt(string path)
    {
        var workspace = new AdhocWorkspace();
        return workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "P", "P", LanguageNames.CSharp, filePath: path));
    }

    private static PatternResult Rewrite(IEnumerable<MetadataReference> extra)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], [.. EPPlusReferences.EPPlus, .. extra]).GetSemanticModel(tree);
        var pattern = new PackagePattern();
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem();
        return pattern.Rewrite(node, model);
    }

    private static PortableExecutableReference Fonts(string version)
    {
        var compilation = CSharpCompilation.Create("SixLabors.Fonts",
            [CSharpSyntaxTree.ParseText($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")] namespace SixLabors.Fonts {{ public class Font {{ }} }}",
                cancellationToken: TestContext.Current.CancellationToken)],
            Helpers.TestReferences.Net, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
