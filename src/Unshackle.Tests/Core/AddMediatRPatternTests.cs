using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class AddMediatRPatternTests
{
    [Theory]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers));", "_ = handlers;")]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(handlers, typeof(C).Assembly));", "_ = handlers;")]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers)); _ = handlers.FullName;", null)]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(C).Assembly));", null)]
    public void Rewrite_DroppedAssemblyParameterInStatement_DiscardsParameter(string body, string? discard)
    {
        // Arrange
        (AddMediatRPattern pattern, SyntaxNode node, SemanticModel model) = Arrange(body);

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.Warning.ShouldBeNull();
        List<string> statements = [.. fixedResult.NewNodes.Select(n => n.ToString())];
        if (discard is null)
        {
            statements.Count.ShouldBe(1);
        }
        else
        {
            statements.ShouldBe([discard, statements[1]]);
            statements[1].ShouldStartWith("services.AddMediator(");
        }
    }

    [Theory]
    [InlineData("var s = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers));", "handlers")]
    [InlineData("var s = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(C).Assembly));", null)]
    public void Rewrite_DroppedAssemblyParameterInExpression_FixedWithWarning(string body, string? parameter)
    {
        // Arrange
        (AddMediatRPattern pattern, SyntaxNode node, SemanticModel model) = Arrange(body);

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        if (parameter is null)
        {
            fixedResult.Warning.ShouldBeNull();
        }
        else
        {
            fixedResult.Warning.ShouldNotBeNull().ShouldContain($"Parameter '{parameter}' is no longer used");
        }
    }

    [Fact]
    public void Rewrite_AssemblyParameterOfPublicMethod_Manual()
    {
        // Arrange
        (AddMediatRPattern pattern, SyntaxNode node, SemanticModel model) =
            Arrange("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers));", "public class C { public");

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("'handlers' of a public method");
    }

    [Theory]
    [InlineData("Shop.Tests", typeof(ManualResult))]
    [InlineData("DynamicProxyGenAssembly2", typeof(FixedResult))]
    public void Rewrite_AssemblyParameterOfInternalMethodWithFriend_ManualUnlessMockingProxy(string friend, Type expected)
    {
        // Arrange
        (AddMediatRPattern pattern, SyntaxNode node, SemanticModel model) = Arrange(
            "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers));",
            $"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{friend}, PublicKey=00\")]\ninternal class C {{ internal");

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType(expected);
        if (result is ManualResult manual)
        {
            manual.Reason.ShouldContain("'handlers' of an internal method that Shop.Tests may call");
        }
    }

    [Theory]
    [InlineData("internal", "", typeof(ManualResult))]
    [InlineData("public", "", typeof(FixedResult))]
    [InlineData("internal", "Other", typeof(ManualResult))]
    [InlineData("internal", "Host", typeof(FixedResult))]
    public void Rewrite_ScannedAssemblyOfOtherProject_ManualWhenHandlerNotPublicAndNoFriend(string accessibility, string friend, Type expected)
    {
        // Arrange
        CancellationToken token = TestContext.Current.CancellationToken;
        SyntaxTree handlers = CSharpSyntaxTree.ParseText(
            "using System.Threading; using System.Threading.Tasks; using MediatR;\n"
            + (friend.Length > 0 ? $"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{friend}\")]\n" : "")
            + "public class Marker { }\npublic class Ping : IRequest { }\n"
            + accessibility + " class PingHandler : IRequestHandler<Ping> { public Task Handle(Ping request, CancellationToken cancellationToken) => Task.CompletedTask; }",
            cancellationToken: token);
        MetadataReference library = CSharpCompilation.Create("Handlers", [handlers], TestReferences.MediatR,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).ToMetadataReference();
        SyntaxTree host = CSharpSyntaxTree.ParseText(
            "using Microsoft.Extensions.DependencyInjection;\nclass C { void T(IServiceCollection services) { var scanned = typeof(Marker).Assembly; "
            + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(scanned)); } }",
            cancellationToken: token);
        SemanticModel model = CSharpCompilation.Create("Host", [host], [.. TestReferences.MediatR, library]).GetSemanticModel(host);
        var pattern = new AddMediatRPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(host.GetRoot(token), model).Single(), model);

        // Assert
        result.ShouldBeOfType(expected);
        if (result is ManualResult manual)
        {
            manual.Reason.ShouldContain("PingHandler is not public");
        }
    }

    [Theory]
    [InlineData("Library.Registration.T(services, typeof(Use).Assembly);", typeof(ManualResult))]
    [InlineData("", typeof(FixedResult))]
    public async Task RewriteAsync_InternalMethodWithFriend_ManualOnlyWhenOtherProjectCallsIt(string call, Type expected)
    {
        // Arrange
        CancellationToken token = TestContext.Current.CancellationToken;
        var library = ProjectId.CreateNewId();
        var friend = ProjectId.CreateNewId();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var registration = DocumentId.CreateNewId(library);
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(library, "Library", "Library", LanguageNames.CSharp).WithProjectCompilationOptions(library, options)
            .AddMetadataReferences(library, TestReferences.MediatR)
            .AddDocument(registration, "Registration.cs",
                "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\n[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Friend\")]\n"
                + "namespace Library;\ninternal static class Registration { internal static void T(IServiceCollection services, Assembly handlers) { "
                + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers)); } }")
            .AddProject(friend, "Friend", "Friend", LanguageNames.CSharp).WithProjectCompilationOptions(friend, options)
            .AddMetadataReferences(friend, TestReferences.MediatR)
            .AddProjectReference(friend, new ProjectReference(library))
            .AddDocument(DocumentId.CreateNewId(friend), "Use.cs",
                "using Microsoft.Extensions.DependencyInjection;\nclass Use { void M(IServiceCollection services) { " + call + " } }", filePath: "Use.cs");
        Document document = solution.GetDocument(registration)!;
        SemanticModel model = (await document.GetSemanticModelAsync(token))!;
        var pattern = new AddMediatRPattern();
        SyntaxNode node = pattern.Find((await document.GetSyntaxRootAsync(token))!, model).Single();

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, token);

        // Assert
        result.ShouldBeOfType(expected);
        if (result is ManualResult manual)
        {
            manual.Reason.ShouldContain("'handlers' of an internal method that Friend calls (Use.cs:2)");
        }
    }

    [Theory]
    [InlineData("public static IServiceCollection AddPublisher(this IServiceCollection services, Assembly assembly) => services.AddInMemory(assembly);", typeof(ManualResult))]
    [InlineData("public static IServiceCollection AddPublisher(this IServiceCollection services) => services.AddInMemory(typeof(Module).Assembly);", typeof(FixedResult))]
    [InlineData("", typeof(FixedResult))]
    public async Task RewriteAsync_InternalScanFedByPublicParameter_ManualNamingThePublicMethod(string wrapper, Type expected)
    {
        // Arrange (starter, TAAK-290: a public wrapper in a shared library passes the host's assembly to an internal AddMediatR method)
        CancellationToken token = TestContext.Current.CancellationToken;
        var library = ProjectId.CreateNewId();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var registration = DocumentId.CreateNewId(library);
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(library, "Common", "Common", LanguageNames.CSharp).WithProjectCompilationOptions(library, options)
            .AddMetadataReferences(library, TestReferences.MediatR)
            .AddDocument(registration, "Module.cs",
                "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\nnamespace Common;\n"
                + "internal static class Module { internal static IServiceCollection AddInMemory(this IServiceCollection services, Assembly assembly) { "
                + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly)); return services; } }")
            .AddDocument(DocumentId.CreateNewId(library), "Publisher.cs",
                "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\nnamespace Common;\npublic static class Publisher { " + wrapper + " }");
        Document document = solution.GetDocument(registration)!;
        SemanticModel model = (await document.GetSemanticModelAsync(token))!;
        var pattern = new AddMediatRPattern();
        SyntaxNode node = pattern.Find((await document.GetSyntaxRootAsync(token))!, model).Single();

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, token);

        // Assert
        result.ShouldBeOfType(expected);
        if (result is ManualResult manual)
        {
            manual.Reason.ShouldContain("'assembly', which comes from a parameter of the public method Publisher.AddPublisher");
        }
    }

    [Theory]
    [InlineData("NetArchTest.Rules.dll", true)]
    [InlineData("ArchUnitNET.xUnit.dll", true)]
    [InlineData("Other.dll", false)]
    public async Task RewriteAsync_SolutionWithArchitectureTests_WarnsAboutMediatorNamespace(string library, bool warns)
    {
        // Arrange
        CancellationToken token = TestContext.Current.CancellationToken;
        var host = ProjectId.CreateNewId();
        var tests = ProjectId.CreateNewId();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        using var image = new MemoryStream();
        CSharpCompilation.Create(Path.GetFileNameWithoutExtension(library), [], TestReferences.MediatR, options).Emit(image, cancellationToken: token);
        var registration = DocumentId.CreateNewId(host);
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(host, "Host", "Host", LanguageNames.CSharp).WithProjectCompilationOptions(host, options)
            .AddMetadataReferences(host, TestReferences.MediatR)
            .AddDocument(registration, "Registration.cs",
                "using Microsoft.Extensions.DependencyInjection;\nclass Registration { void T(IServiceCollection services) { "
                + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly)); } }")
            .AddProject(tests, "ArchTests", "ArchTests", LanguageNames.CSharp)
            .AddMetadataReference(tests, MetadataReference.CreateFromImage(image.ToArray(), filePath: Path.Combine("lib", library)));
        Document document = solution.GetDocument(registration)!;
        SemanticModel model = (await document.GetSemanticModelAsync(token))!;
        var pattern = new AddMediatRPattern();
        SyntaxNode node = pattern.Find((await document.GetSyntaxRootAsync(token))!, model).Single();

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, token);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        if (warns)
        {
            fixedResult.Warning.ShouldNotBeNull().ShouldContain("ArchTests uses an architecture-test library");
            fixedResult.Warning.ShouldContain(".And().DoNotResideInNamespace(\"Mediator\")");
        }
        else
        {
            fixedResult.Warning.ShouldBeNull();
        }
    }

    private static (AddMediatRPattern Pattern, SyntaxNode Node, SemanticModel Model) Arrange(string body, string declaration = "class C {")
    {
        string code = "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\n"
            + declaration + " void T(IServiceCollection services, Assembly handlers) { " + body + " } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        var pattern = new AddMediatRPattern();
        return (pattern, pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single(), model);
    }
}
