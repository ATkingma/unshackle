using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.AutoMapper;

// automapper.generic-profile (TAAK-334, council round 15 A14): a generic profile is closed per type argument, or Manual GenericProfileOpenSet.
public class GenericProfilePatternTests
{
    private const string ProjectFile = "Shop.csproj";

    private const string Types = """
        using AutoMapper;

        namespace Shop;

        public class Role
        {
            public int Id { get; set; }
        }

        public class Tenant
        {
            public int Id { get; set; }
        }

        ACCESS class CopyProfile<T> : Profile
        {
            public CopyProfile()
            {
                MAPS
            }
        }
        """;

    [Theory]
    [InlineData("internal class RoleProfile : CopyProfile<Role> { }")]
    [InlineData("internal static class Setup { public static object Make() => new CopyProfile<Role>(); }")]
    [InlineData("internal static class Setup { public static void Add(IMapperConfigurationExpression cfg) => cfg.AddProfile<CopyProfile<Role>>(); }")]
    public async Task FixAsync_ClosedConstruction_WritesMethodPerPair(string use)
    {
        // Arrange
        Solution solution = Single(Profile("internal", "CreateMap<T, T>();"), "using AutoMapper;\n\nnamespace Shop;\n\n" + use);

        // Act
        FixReport report = await FixAsync(new AutoMapperFix(), solution);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
        report.Findings.ShouldContain(f => f.PatternId == "automapper.generic-profile");
        string types = await TextAsync(report.Solution, "Types.cs");
        types.ShouldContain("public static Role ToRole(this Role source) => new()");
        types.ShouldNotContain("CreateMap<T, T>()");
    }

    [Fact]
    public async Task FixAsync_TwoClosedSubclasses_WritesBothPairs()
    {
        // Arrange
        Solution solution = Single(Profile("internal", "CreateMap<T, T>();"),
            "namespace Shop;\n\ninternal class RoleProfile : CopyProfile<Role> { }\n\ninternal class TenantProfile : CopyProfile<Tenant> { }");

        // Act
        FixReport report = await FixAsync(new AutoMapperFix(), solution);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
        string types = await TextAsync(report.Solution, "Types.cs");
        types.ShouldContain("ToRole(this Role source)");
        types.ShouldContain("ToTenant(this Tenant source)");
    }

    [Theory]
    [InlineData("CreateMap<T, T>().ReverseMap();", "is not a CreateMap<A, B>() without options")]
    [InlineData("CreateMap<System.Collections.Generic.List<T>, System.Collections.Generic.List<T>>();", "is not closed to plain types")]
    public async Task FixAsync_UnsupportedMap_IsOpenSet(string maps, string reason)
    {
        // Arrange
        Solution solution = Single(Profile("internal", maps), "namespace Shop;\n\ninternal class RoleProfile : CopyProfile<Role> { }");

        // Act
        FixReport report = await FixAsync(new AutoMapperFix(), solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-profile").Result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe("GenericProfileOpenSet");
        manual.Reason.ShouldContain(reason);
    }

    [Fact]
    public async Task FixAsync_NoClosedConstruction_IsOpenSet()
    {
        // Arrange
        Solution solution = Single(Profile("internal", "CreateMap<T, T>();"));

        // Act
        FixReport report = await FixAsync(new AutoMapperFix(), solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-profile").Result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe("GenericProfileOpenSet");
        manual.Reason.ShouldContain("no caller in the solution closes T");
    }

    // R1 with project properties: IsPackable=false closes a public generic profile; a packable one names the A13 fix.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FixAsync_PublicProfile_FollowsIsPackable(bool packable)
    {
        // Arrange
        Solution solution = Single(Profile("public", "CreateMap<T, T>();"), "namespace Shop;\n\npublic class RoleProfile : CopyProfile<Role> { }");
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.IsPackable(ProjectFile).Returns(packable);

        // Act
        FixReport report = await FixAsync(new AutoMapperFix(properties), solution);

        // Assert
        Finding finding = report.Findings.Single(f => f.PatternId == "automapper.generic-profile");
        if (packable)
        {
            ManualResult manual = finding.Result.ShouldBeOfType<ManualResult>();
            manual.Code.ShouldBe("GenericProfileOpenSet");
            manual.Reason.ShouldContain("Set `<IsPackable>false</IsPackable>` in Shop.csproj if this library is not published");
        }
        else
        {
            report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
        }
    }

    private static string Profile(string access, string maps) => Types.Replace("ACCESS", access).Replace("MAPS", maps);

    private static Solution Single(params string[] files)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(project, VersionStamp.Default, "Shop", "Shop", LanguageNames.CSharp, filePath: ProjectFile))
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(project, new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(project, TestReferences.AutoMapper);
        string[] names = ["Types.cs", "Uses.cs"];
        for (int i = 0; i < files.Length; i++)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(project), names[i], files[i], filePath: names[i]);
        }

        return solution;
    }

    private static Task<FixReport> FixAsync(AutoMapperFix fix, Solution solution) =>
        new Fixer([fix]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

    private static string Reasons(FixReport report) =>
        string.Join(" | ", report.Findings.Select(f => f.PatternId + ": " + (f.Result is ManualResult m ? m.Reason : f.Result.GetType().Name)));

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
