using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// Maps in `new MapperConfiguration(cfg => ...)` outside a Profile (TAAK-286): the lambda takes the place of the Profile constructor.
public class ConfigurationMapsTests
{
    private const string Types = """
        using AutoMapper;

        namespace Shop;

        public class Item
        {
            public int Id { get; set; }
        }

        public class ItemDto
        {
            public int Id { get; set; }
        }

        """;

    [Fact]
    public async Task FixAsync_ExpressionLambda_BecomesExtensionMethod()
    {
        // Arrange
        string code = Types + """
            public static class Config
            {
                public static readonly MapperConfiguration Value = new(cfg => cfg.CreateMap<Item, ItemDto>());
            }
            """;

        // Act
        FixReport report = await FixAsync(code);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
        (await Text(report)).ShouldContain("public static ItemDto ToItemDto(this Item source)");
    }

    [Fact]
    public async Task FixAsync_SettingInLambda_StaysManual()
    {
        // Arrange
        string code = Types + """
            public static class Config
            {
                public static readonly MapperConfiguration Value = new(cfg =>
                {
                    cfg.AllowNullCollections = true;
                    cfg.CreateMap<Item, ItemDto>();
                });
            }
            """;

        // Act
        FixReport report = await FixAsync(code);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult);
        (await Text(report)).ShouldNotContain("ToItemDto");
    }

    [Fact]
    public async Task FixAsync_TwoConfigurationsWithMaps_StaysManual()
    {
        // Arrange
        string code = Types + """
            public static class Config
            {
                public static readonly MapperConfiguration One = new(cfg => cfg.CreateMap<Item, ItemDto>());

                public static readonly MapperConfiguration Two = new(cfg => cfg.CreateMap<ItemDto, Item>());
            }
            """;

        // Act
        FixReport report = await FixAsync(code);

        // Assert
        report.Findings.Select(f => f.Result).OfType<ManualResult>().ShouldContain(m => m.Reason.Contains("several MapperConfiguration"));
    }

    private static async Task<FixReport> FixAsync(string code)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(project, TestReferences.AutoMapper)
            .AddDocument(DocumentId.CreateNewId(project), "Config.cs", code, filePath: "Config.cs");
        return await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);
    }

    private static async Task<string> Text(FixReport report) =>
        (await report.Solution.Projects.Single().Documents.Single().GetTextAsync(TestContext.Current.CancellationToken)).ToString();

    private static string Reasons(FixReport report) => string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId));
}
