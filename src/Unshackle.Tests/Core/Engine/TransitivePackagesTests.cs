using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Engine;

// TAAK-329, council round 16 G10: a package that came only with the removed one is pinned where code (or a dependent's code) still uses it.
public class TransitivePackagesTests
{
    private const string Domain = "/repo/Domain/Domain.csproj";
    private const string Infrastructure = "/repo/Infrastructure/Infrastructure.csproj";
    private const string Configuration = "Microsoft.Extensions.Configuration";
    private const string ConfigurationPath = "/nuget/microsoft.extensions.configuration/8.0.0/lib/net8.0/Microsoft.Extensions.Configuration.dll";

    private static readonly PackageMigration s_migration = new()
    {
        RemovedPackage = "EPPlus",
        AddedPackages = [new AddedPackage { Name = "ClosedXML", Version = "0.105.1" }],
    };

    [Fact]
    public void Lost_PackageOnlyThroughRemoved_IsLostOthersStay()
    {
        // Arrange
        PackageAssets assets = Assets(
            Requirement("EPPlus", "project"),
            Requirement("Serilog", "project"),
            Requirement(Configuration, "EPPlus/7.1.3"),
            Requirement("Microsoft.Extensions.Primitives", $"{Configuration}/8.0.0"),
            Requirement("System.Shared", "EPPlus/7.1.3"),
            Requirement("System.Shared", "Serilog/3.0.0"),
            Requirement("Serilog", "EPPlus/7.1.3"));

        // Act
        IReadOnlyList<string> lost = TransitivePackages.Lost(assets, ["EPPlus"]);

        // Assert
        lost.ShouldBe([Configuration, "Microsoft.Extensions.Primitives"]);
    }

    [Theory]
    [InlineData(ConfigurationPath, Configuration)]
    [InlineData("C:\\Users\\u\\.nuget\\packages\\Microsoft.Extensions.Configuration\\8.0.0\\lib\\x.dll", Configuration)]
    [InlineData("/nuget/microsoft.extensions.configuration/9.0.0/lib/x.dll", null)]
    [InlineData("/dotnet/packs/Microsoft.AspNetCore.App.Ref/8.0.0/ref/x.dll", null)]
    public void PackageOf_PathInPackagesFolder_FindsPackageAtResolvedVersion(string path, string? expected)
    {
        // Arrange
        var lost = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [Configuration] = "8.0.0" };

        // Act
        string? package = TransitivePackages.PackageOf(path, lost);

        // Assert
        package.ShouldBe(expected);
    }

    [Theory]
    [InlineData("using Microsoft.Extensions.Configuration;\nclass C { IConfiguration? _c; }", true)]
    [InlineData("using Microsoft.Extensions.Configuration;\nclass C { }", true)]
    [InlineData("class C { }", false)]
    public async Task PlanAsync_DependentUsesLostPackage_PinsInMigratedProject(string infrastructureCode, bool pinned)
    {
        // Arrange
        Solution solution = Create(infrastructureCode);
        IPackageAssetsReader reader = Reader(AssetsState.Current);

        // Act
        TransitivePackages plan = await TransitivePackages.PlanAsync(solution, [Migration(keep: false)], reader, TestContext.Current.CancellationToken);

        // Assert
        IReadOnlyList<ProjectFileEdit> edits = plan.EditsFor(solution.Projects.Single(p => p.FilePath == Domain));
        if (pinned)
        {
            PackageAddEdit edit = edits.ShouldHaveSingleItem().ShouldBeOfType<PackageAddEdit>();
            edit.Name.ShouldBe(Configuration);
            edit.Version.ShouldBe("8.0.0");
            edit.KeepsTransitiveOf.ShouldBe("EPPlus");
        }
        else
        {
            edits.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData(AssetsState.Stale, false)]
    [InlineData(AssetsState.Missing, false)]
    [InlineData(AssetsState.Current, true)]
    public async Task PlanAsync_KeptPackageOrNoCurrentAssets_PinsNothing(AssetsState state, bool keep)
    {
        // Arrange
        Solution solution = Create("using Microsoft.Extensions.Configuration;\nclass C { IConfiguration? _c; }");

        // Act
        TransitivePackages plan = await TransitivePackages.PlanAsync(solution, [Migration(keep)], Reader(state), TestContext.Current.CancellationToken);

        // Assert
        plan.HasEdits.ShouldBeFalse();
    }

    [Fact]
    public async Task FixAsync_WithAssetsReader_ReportsOneFixedRowPerPin()
    {
        // Arrange
        Solution solution = Create("using Microsoft.Extensions.Configuration;\nclass C { IConfiguration? _c; }");
        IProjectFileStore store = Substitute.For<IProjectFileStore, IProjectFileEditor>();
        var editor = (IProjectFileEditor)store;
        store.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([]);
        editor.ApplyAsync(Domain, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .Returns(c => [.. c.ArgAt<IEnumerable<ProjectFileEdit>>(1).Select(e => new ProjectFileEditOutcome { Edit = e, FilePath = Domain, Line = 5 })]);
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("epplus");
        fix.Package.Returns(s_migration);
        fix.Patterns.Returns([]);
        fix.RemovedNamespaces.Returns(["OfficeOpenXml"]);
        fix.AddedNamespaces.Returns([]);
        fix.AddedReferences.Returns([]);
        fix.SwappedTypes.Returns([]);
        var fixer = new Fixer([fix], store, Reader(AssetsState.Current));

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        Finding row = report.Findings.Single(f => f.PatternId == Fixer.ProjectFilePatternId && f.Line == 5);
        row.FilePath.ShouldBe(Domain);
        row.Result.ShouldBeOfType<FixedResult>().Info.ShouldBe(
            $"may conflict (NU1605): PackageReference {Configuration} pinned to 8.0.0, kept for `Infrastructure` (it came transitively with EPPlus). "
            + $"If restore reports NU1605 for {Configuration}, raise the pin");
    }

    [Theory]
    [InlineData("[8.0.0, )", true)]
    [InlineData("[9.0.0, )", false)]
    [InlineData("7.0.0", false)]
    public async Task PlanAsync_ProjectReferenceBringsPackage_PinsSameVersionOtherwiseManual(string range, bool pinned)
    {
        // Arrange (council round 17 T3)
        Solution solution = Create("using Microsoft.Extensions.Configuration;\nclass C { IConfiguration? _c; }");
        PackageRequirement fromReference = new() { Package = Configuration, RequiredBy = "Shared/1.0.0", Range = range };

        // Act
        TransitivePackages plan = await TransitivePackages.PlanAsync(
            solution, [Migration(keep: false)], Reader(AssetsState.Current, fromReference), TestContext.Current.CancellationToken);

        // Assert
        plan.HasEdits.ShouldBe(pinned);
        if (pinned)
        {
            plan.Manuals.ShouldBeEmpty();
        }
        else
        {
            (string path, ManualResult manual) = plan.Manuals.ShouldHaveSingleItem();
            path.ShouldBe(Domain);
            manual.Reason.ShouldContain($"project reference Shared/1.0.0 brings {Configuration} {range}");
        }
    }

    private static ProjectMigration Migration(bool keep) => new()
    {
        ProjectPath = Domain,
        Package = s_migration,
        KeepRemovedPackage = keep,
        RemovedNamespaces = [],
        AddedNamespaces = [],
    };

    private static IPackageAssetsReader Reader(AssetsState state, params PackageRequirement[] projectReferenceRequirements)
    {
        IPackageAssetsReader reader = Substitute.For<IPackageAssetsReader>();
        reader.Read(Domain).Returns(Assets(Requirement("EPPlus", "project"), Requirement(Configuration, "EPPlus/7.1.3")) is var assets
            ? new PackageAssets { State = state, Resolved = assets.Resolved, Requirements = assets.Requirements, ProjectReferenceRequirements = projectReferenceRequirements }
            : PackageAssets.Missing);
        reader.Read(Infrastructure).Returns(PackageAssets.Missing);
        return reader;
    }

    private static PackageAssets Assets(params PackageRequirement[] requirements) => new()
    {
        State = AssetsState.Current,
        Resolved = requirements.Select(r => r.Package).Distinct().ToDictionary(p => p, _ => "8.0.0", StringComparer.OrdinalIgnoreCase),
        Requirements = requirements,
    };

    private static PackageRequirement Requirement(string package, string by) => new() { Package = package, RequiredBy = by, Range = "[8.0.0, )" };

    // Domain (declares EPPlus, no code using it) and Infrastructure (references Domain); both see the configuration assembly from the packages folder.
    private static Solution Create(string infrastructureCode)
    {
        MetadataReference configuration = Library(Configuration, "namespace Microsoft.Extensions.Configuration { public interface IConfiguration { } }", ConfigurationPath);
        MetadataReference epplus = Library("EPPlus", "namespace OfficeOpenXml { public class ExcelPackage { } }", "/nuget/epplus/7.1.3/lib/net8.0/EPPlus.dll");
        var domain = ProjectId.CreateNewId();
        var infrastructure = ProjectId.CreateNewId();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        return TestSolution.NewEmpty()
            .AddProject(ProjectInfo.Create(domain, VersionStamp.Default, "Domain", "Domain", LanguageNames.CSharp, filePath: Domain, compilationOptions: options,
                metadataReferences: [.. TestReferences.Net, configuration, epplus]))
            .AddDocument(DocumentId.CreateNewId(domain), "D.cs", "class D { }", filePath: "/repo/Domain/D.cs")
            .AddProject(ProjectInfo.Create(infrastructure, VersionStamp.Default, "Infrastructure", "Infrastructure", LanguageNames.CSharp, filePath: Infrastructure,
                compilationOptions: options, metadataReferences: [.. TestReferences.Net, configuration], projectReferences: [new ProjectReference(domain)]))
            .AddDocument(DocumentId.CreateNewId(infrastructure), "I.cs", infrastructureCode, filePath: "/repo/Infrastructure/I.cs");
    }

    private static PortableExecutableReference Library(string name, string code, string path)
    {
        var compilation = CSharpCompilation.Create(name,
            [CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken)],
            TestReferences.Net, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        return MetadataReference.CreateFromImage(stream.ToArray(), filePath: path);
    }
}
