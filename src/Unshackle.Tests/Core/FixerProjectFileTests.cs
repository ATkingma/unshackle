using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class FixerProjectFileTests
{
    private const string ProjectPath = "/repo/Tests/Tests.csproj";
    private const string StillUsesFluentAssertions = "using FluentAssertions; class C { void M() => 1.Should(); }";
    private const string NoFluentAssertions = "using FluentAssertions; class C { void M() { } }";

    private static readonly PackageMigration s_package =
        new() { RemovedPackage = "FluentAssertions", AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }] };

    private readonly IProjectFileStore _projectFiles = Substitute.For<IProjectFileStore>();
    private readonly ProjectFileChange _change = new() { FilePath = ProjectPath, OldText = "<Project />", NewText = "<Project></Project>" };

    public FixerProjectFileTests()
    {
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([_change]);
    }

    [Fact]
    public async Task FixAsync_CodeNoLongerUsesPackage_RemovesItAndReportsFixedProjectFile()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, NoFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().ProjectPath == ProjectPath && !m.Single().KeepRemovedPackage
                && m.Single().Package == s_package && m.Single().AddedNamespaces.Single() == "Shouldly"),
            Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBe([_change]);
        Finding finding = report.Findings.ShouldHaveSingleItem();
        finding.FilePath.ShouldBe(ProjectPath);
        finding.Line.ShouldBe(0);
        finding.PatternId.ShouldBe(Fixer.ProjectFilePatternId);
        finding.Result.ShouldBeOfType<FixedResult>();
    }

    [Fact]
    public async Task FixAsync_CodeStillUsesPackage_KeepsItAndReportsManual()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, StillUsesFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().KeepRemovedPackage),
            Arg.Any<CancellationToken>());
        Finding manual = report.Findings.Single(f => f.Result is ManualResult && f.PatternId == Fixer.ProjectFilePatternId);
        manual.FilePath.ShouldBe(ProjectPath);
        manual.Line.ShouldBe(0);
        ((ManualResult)manual.Result).Reason.ShouldContain("FluentAssertions is still used");
    }

    [Fact]
    public async Task FixAsync_CodeStillUsesPackageWithoutFinding_ReportsManualOnThatLine()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);
        string code = "using FluentAssertions;\nclass C\n{\n    void M() => 1.Should();\n}";

        // Act
        FixReport report = await FixAsync(fixer, code, TestReferences.FluentAssertions);

        // Assert
        Finding unmatched = report.Findings.Single(f => f.PatternId == Fixer.UnmatchedPatternId);
        unmatched.FilePath.ShouldBe("/repo/Tests/C.cs");
        unmatched.Line.ShouldBe(4);
        ((ManualResult)unmatched.Result).Reason.ShouldContain("is still used here and no pattern migrates it");
    }

    [Fact]
    public async Task FixAsync_CodeNoLongerUsesPackage_ReportsNoUnmatchedUse()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, NoFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        report.Findings.ShouldNotContain(f => f.PatternId == Fixer.UnmatchedPatternId);
    }

    [Fact]
    public async Task FixAsync_ProjectWithoutRemovedAssembly_DoesNotEditProjectFiles()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, "class C { }", TestReferences.Net);

        // Assert
        await _projectFiles.DidNotReceive().EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBeEmpty();
        report.Findings.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_FixWithoutPackage_DoesNotEditProjectFiles()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(null)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, StillUsesFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        await _projectFiles.DidNotReceive().EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_SharedFileKeptForUnloadedProject_ReportsManual()
    {
        // Arrange
        var kept = new ProjectFileChange { FilePath = "/repo/Directory.Packages.props", OldText = "a", NewText = "b", KeptRemovedPackageFor = "/repo/Other/Other.csproj" };
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([_change, kept]);
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, NoFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        report.ProjectFileChanges.ShouldBe([_change, kept]);
        report.Findings.Single(f => f.FilePath == ProjectPath).Result.ShouldBeOfType<FixedResult>();
        ManualResult manual = report.Findings.Single(f => f.FilePath == kept.FilePath).Result.ShouldBeOfType<ManualResult>();
        manual.Reason.ShouldContain("/repo/Other/Other.csproj is not migrated but still uses FluentAssertions");
    }

    [Fact]
    public async Task FixAsync_EditThrows_ReportsManualInsteadOfFailing()
    {
        // Arrange
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new System.Xml.XmlException("bad xml"));
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, NoFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        report.ProjectFileChanges.ShouldBeEmpty();
        ManualResult manual = report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>();
        manual.Reason.ShouldContain("bad xml");
    }

    [Fact]
    public async Task FixAsync_EditThrows_LeavesTheCodeOfThatProjectUnchanged()
    {
        // Arrange
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidDataException("bad formatting"));
        IFix fix = CreateFix(s_package);
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>());
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(PatternResult.Fixed(SyntaxFactory.ParseExpression("System.Console.WriteLine()")));
        fix.Patterns.Returns([pattern]);
        var fixer = new Fixer([fix], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, StillUsesFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        string text = (await report.Solution.Projects.Single().Documents.Single().GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        text.ShouldBe(StillUsesFluentAssertions);
        report.Findings.ShouldNotBeEmpty();
        report.Findings.ShouldAllBe(f => f.Result is ManualResult);
        ((ManualResult)report.Findings.Single(f => f.PatternId == "test.pattern").Result).Reason.ShouldContain("bad formatting");
    }

    [Fact]
    public async Task FixAsync_FileNotUtf8_LeavesItAloneAndMigratesTheRest()
    {
        // Arrange
        IFix fix = CreateFix(s_package);
        IPattern rewriting = CreateRewritingPattern();
        fix.Patterns.Returns([rewriting]);
        var fixer = new Fixer([fix], _projectFiles);
        string latin1Code = StillUsesFluentAssertions + " // café";


        // Act
        FixReport report = await FixAsync(fixer, TestReferences.FluentAssertions, ("A.cs", StillUsesFluentAssertions, Encoding.UTF8), ("L.cs", latin1Code, Encoding.Latin1));

        // Assert
        Dictionary<string, string> texts = await TextsAsync(report);
        texts["L.cs"].ShouldBe(latin1Code);
        texts["A.cs"].ShouldContain("System.Console.WriteLine()");
        Finding encoding = report.Findings.Single(f => f.PatternId == Fixer.EncodingPatternId);
        encoding.FilePath.ShouldBe("/repo/Tests/L.cs");
        encoding.Line.ShouldBe(0);
        ((ManualResult)encoding.Result).Reason.ShouldContain("not UTF-8");
        report.Findings.ShouldNotContain(f => f.PatternId == "test.pattern" && f.FilePath.EndsWith("L.cs", StringComparison.Ordinal));
        // The file still uses the old library, so the project keeps the package.
        await _projectFiles.Received(1).EditAsync(Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().KeepRemovedPackage), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_FileNotUtf8WithoutOldLibrary_ReportsNothingForIt()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, TestReferences.FluentAssertions, ("L.cs", "class L { } // café", Encoding.Latin1));

        // Assert
        report.Findings.ShouldNotContain(f => f.PatternId == Fixer.EncodingPatternId);
    }

    [Fact]
    public async Task FixAsync_FileUtf16_IsNotUtf8EitherAndLeftAlone()
    {
        // Arrange
        IFix fix = CreateFix(s_package);
        IPattern rewriting = CreateRewritingPattern();
        fix.Patterns.Returns([rewriting]);
        var fixer = new Fixer([fix], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, TestReferences.FluentAssertions, ("W.cs", StillUsesFluentAssertions, Encoding.Unicode));

        // Assert
        (await TextsAsync(report))["W.cs"].ShouldBe(StillUsesFluentAssertions);
        report.Findings.ShouldContain(f => f.PatternId == Fixer.EncodingPatternId);
    }

    private static IPattern CreateRewritingPattern()
    {
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>());
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(PatternResult.Fixed(SyntaxFactory.ParseExpression("System.Console.WriteLine()")));
        return pattern;
    }

    private static async Task<Dictionary<string, string>> TextsAsync(FixReport report)
    {
        var texts = new Dictionary<string, string>();
        foreach (Document document in report.Solution.Projects.Single().Documents)
        {
            texts[document.Name] = (await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        }

        return texts;
    }

    private static Task<FixReport> FixAsync(Fixer fixer, IEnumerable<MetadataReference> references, params (string Name, string Code, Encoding Encoding)[] files)
    {
        var projectId = ProjectId.CreateNewId();
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Tests", "Tests", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(references));
        foreach ((string name, string code, Encoding encoding) in files)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), name, SourceText.From(code, encoding), filePath: "/repo/Tests/" + name);
        }

        return fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);
    }

    private static IFix CreateFix(PackageMigration? package)
    {
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        fix.AddedNamespaces.Returns(["Shouldly"]);
        fix.Package.Returns(package);
        return fix;
    }

    // TestSolution has no project path; the Fixer only migrates projects that live in a file.
    private static Task<FixReport> FixAsync(Fixer fixer, string code, IEnumerable<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Tests", "Tests", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(references))
            .AddDocument(DocumentId.CreateNewId(projectId), "C.cs", code, filePath: "/repo/Tests/C.cs");
        return fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);
    }
}
