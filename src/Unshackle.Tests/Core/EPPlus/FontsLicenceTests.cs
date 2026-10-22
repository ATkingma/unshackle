using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
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
    public void ProjectBlockers_ResolvedFontsVersion_LicensedDependencyFromTwo(string version, int expected)
    {
        // Arrange
        Project project = Helpers.TestSolution.Create(Code, [.. EPPlusReferences.EPPlus, Fonts(version)]).Projects.Single();

        // Act
        IReadOnlyList<ManualResult> blockers = new Unshackle.Core.Fixes.EPPlus.EPPlusFix().ProjectBlockers(project);

        // Assert
        blockers.Count.ShouldBe(expected);
        blockers.ShouldAllBe(b => b.Code == nameof(ProjectBlockingReason.LicensedDependency));
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
