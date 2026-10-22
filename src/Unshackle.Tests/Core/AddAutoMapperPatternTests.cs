using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// Council round 6, Q4: the kept add-automapper row names one example IMapper user; it is the first by file path (ordinal, '\' and '/'
// alike), then line, whatever the order of the syntax trees, in Rewrite and RewriteAsync alike.
public class AddAutoMapperPatternTests
{
    private const string Registration = """
        using AutoMapper;
        using Microsoft.Extensions.DependencyInjection;

        namespace Shop;

        public static class Registration
        {
            public static void AddShop(IServiceCollection services)
            {
                services.AddAutoMapper(typeof(Registration).Assembly);
            }
        }
        """;

    // Two IMapper users; the second file in tree order comes first by path once '\' reads as '/' ("src/a" < "src/b").
    private const string LaterUser = """
        using AutoMapper;

        namespace Shop;

        public class Later
        {
            public IConfigurationProvider Configuration(IMapper mapper) => mapper.ConfigurationProvider;
        }
        """;

    private const string EarlierUser = """
        using AutoMapper;

        namespace Shop;

        public class Earlier
        {

            public IConfigurationProvider Configuration(IMapper mapper) => mapper.ConfigurationProvider;
        }
        """;

    private const string Expected = @"src\a\Earlier.cs:8";

    [Fact]
    public async Task Rewrite_TwoUsersInReverseTreeOrder_NamesTheFirstByPath()
    {
        // Arrange
        (SyntaxNode node, SemanticModel model, _) = await RegistrationAsync();

        // Act
        PatternResult result = new AddAutoMapperPattern().Rewrite(node, model);

        // Assert
        ((ManualResult)result).Reason.ShouldStartWith($"IMapper is still used ({Expected}), so AddAutoMapper stays");
    }

    [Fact]
    public async Task RewriteAsync_TwoUsersInReverseTreeOrder_NamesTheFirstByPath()
    {
        // Arrange
        (SyntaxNode node, SemanticModel model, Solution solution) = await RegistrationAsync();

        // Act
        PatternResult result = await new AddAutoMapperPattern().RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        ((ManualResult)result).Reason.ShouldStartWith($"IMapper is still used ({Expected}), so AddAutoMapper stays");
    }

    private static async Task<(SyntaxNode Node, SemanticModel Model, Solution Solution)> RegistrationAsync()
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(project, TestReferences.AutoMapper);
        foreach ((string path, string text) in new[] { ("src/Registration.cs", Registration), ("src/b/Later.cs", LaterUser), (@"src\a\Earlier.cs", EarlierUser) })
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(project), Path.GetFileName(path.Replace('\\', '/')), text, filePath: path);
        }

        Document document = solution.Projects.Single().Documents.Single(d => d.Name == "Registration.cs");
        SyntaxNode root = await document.GetSyntaxRootAsync(TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("No root.");
        SemanticModel model = await document.GetSemanticModelAsync(TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("No model.");
        return (new AddAutoMapperPattern().Find(root, model).Single(), model, solution);
    }
}
