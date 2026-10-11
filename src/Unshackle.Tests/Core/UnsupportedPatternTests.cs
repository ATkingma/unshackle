using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// A package that builds on MediatR (Ardalis.SharedKernel) needs a second assembly: no proof can show it (TAAK-133).
public class UnsupportedPatternTests
{
    private const string Kernel = """
        namespace Kernel;

        public abstract class DomainEventBase : MediatR.INotification { }
        public interface ICommand<out TResult> : MediatR.IRequest<TResult> { }
        public class Plain { }
        """;

    [Theory]
    [InlineData("public class Deleted : Kernel.DomainEventBase { }")]
    [InlineData("public class Create : Kernel.ICommand<int> { }")]
    public void Rewrite_TypeBuildsOnMediatRThroughAnotherPackage_ReportsManualNamingThePackage(string code)
    {
        // Arrange
        (TypeDeclarationSyntax type, SemanticModel model) = Compile(code);
        var pattern = new UnsupportedPattern();

        // Act
        PatternResult result = pattern.Rewrite(type, model);

        // Assert
        pattern.Find(type.SyntaxTree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem();
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("Kernel");
    }

    [Theory]
    [InlineData("public class Other : Kernel.Plain { }")]
    [InlineData("public class Ping : MediatR.INotification { }")]
    public void Find_NoPackageOnMediatR_FindsNothing(string code)
    {
        // Arrange
        (TypeDeclarationSyntax type, SemanticModel model) = Compile(code);

        // Act + Assert
        new UnsupportedPattern().Find(type.SyntaxTree.GetRoot(TestContext.Current.CancellationToken), model).ShouldBeEmpty();
    }

    private static (TypeDeclarationSyntax Type, SemanticModel Model) Compile(string code)
    {
        // Emitted, not a CompilationReference: a package comes as metadata, a project in the solution as source.
        using var image = new MemoryStream();
        CSharpCompilation.Create("Kernel", [CSharpSyntaxTree.ParseText(Kernel)], TestReferences.MediatR, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .Emit(image, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        MetadataReference kernel = MetadataReference.CreateFromImage(image.ToArray());
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "App", [tree], [.. TestReferences.MediatR, kernel], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return (tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single(), compilation.GetSemanticModel(tree));
    }
}
