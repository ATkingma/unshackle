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
    [InlineData("internal", typeof(ManualResult))]
    [InlineData("public", typeof(FixedResult))]
    public void Rewrite_ScannedAssemblyOfOtherProject_ManualWhenHandlerNotPublic(string accessibility, Type expected)
    {
        // Arrange
        CancellationToken token = TestContext.Current.CancellationToken;
        SyntaxTree handlers = CSharpSyntaxTree.ParseText(
            "using System.Threading; using System.Threading.Tasks; using MediatR;\npublic class Marker { }\npublic class Ping : IRequest { }\n"
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
