using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.MediatR;

// Council round 8, A2: an AddMediatR scan of a parameter or type parameter is Fixed only when every caller in the solution passes the same
// literal typeof(X).Assembly and the method is non-public or in a non-packable project; otherwise Manual (AssemblyNotStaticallyKnown).
public class AssemblyResolverTests
{
    private const string Registration =
        "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\n[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Host\")]\n"
        + "namespace Library;\npublic class Marker { }\n";

    [Theory]
    [InlineData("Library.Registration.T(services, typeof(Library.Marker).Assembly);", null)]
    [InlineData("Library.Registration.T(services, typeof(Use).Assembly);", "which Library does not reference")]
    [InlineData("Library.Registration.T(services, typeof(Library.Marker).Assembly); Library.Registration.T(services, typeof(string).Assembly);", "callers pass different assemblies")]
    [InlineData("Library.Registration.T(services, Assembly.GetExecutingAssembly());", "does not pass a literal typeof(X).Assembly")]
    [InlineData("System.Action<IServiceCollection, Assembly> add = Library.Registration.T; add(services, typeof(Library.Marker).Assembly);", "is used other than as a plain call")]
    [InlineData("System.Action a = () => Library.Registration.T(services, typeof(Library.Marker).Assembly); a();", "in a lambda that is not passed straight to a call")]
    public async Task RewriteAsync_InternalMethodCalledFromFriend_FixedOnlyForOneLiteralVisibleAssembly(string call, string? manual)
    {
        // Arrange
        string library = Registration
            + "internal static class Registration { internal static void T(IServiceCollection services, Assembly handlers) { "
            + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers)); } }";

        // Act
        PatternResult result = await RewriteAsync(library, OutputKind.DynamicallyLinkedLibrary, call);

        // Assert
        AssertResult(result, manual);
    }

    [Theory]
    [InlineData(OutputKind.ConsoleApplication, null)]
    [InlineData(OutputKind.DynamicallyLinkedLibrary, "public in a packable project")]
    public async Task RewriteAsync_PublicMethod_FixedOnlyOutsidePackableProject(OutputKind kind, string? manual)
    {
        // Arrange
        string library = Registration
            + "public static class Registration { public static void T(IServiceCollection services, Assembly handlers) { "
            + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers)); } }\n"
            + "internal static class Program { private static void Main() { } private static void Use(IServiceCollection services) => Registration.T(services, typeof(Marker).Assembly); }";

        // Act
        PatternResult result = await RewriteAsync(library, kind, null);

        // Assert
        AssertResult(result, manual);
    }

    [Theory]
    [InlineData("Library.Registration.T<Library.Marker>(services);", null)]
    [InlineData("Library.Registration.T<Use>(services);", "which Library does not reference")]
    public async Task RewriteAsync_TypeParameter_ResolvedFromExplicitTypeArgument(string call, string? manual)
    {
        // Arrange (miru's shape, made internal)
        string library = Registration
            + "internal static class Registration { internal static void T<TAssemblyOfType>(IServiceCollection services) { "
            + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TAssemblyOfType>()); } }";

        // Act
        PatternResult result = await RewriteAsync(library, OutputKind.DynamicallyLinkedLibrary, call);

        // Assert
        AssertResult(result, manual);
    }

    [Fact]
    public async Task RewriteAsync_PublicForwarderPassesExecutingAssembly_StaysManual()
    {
        // Arrange (starter: a public wrapper in a library forwards Assembly.GetExecutingAssembly() of the host)
        string library = Registration
            + "internal static class Module { internal static void T(IServiceCollection services, Assembly handlers) { "
            + "services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers)); } }\n"
            + "public static class Publisher { public static void Add(IServiceCollection services, Assembly assembly) => Module.T(services, assembly); }";

        // Act
        PatternResult result = await RewriteAsync(library, OutputKind.DynamicallyLinkedLibrary, "Library.Publisher.Add(services, Assembly.GetExecutingAssembly());");

        // Assert
        ManualResult manualResult = result.ShouldBeOfType<ManualResult>();
        manualResult.Reason.ShouldStartWith(AssemblyResolution.AssemblyNotStaticallyKnown + ": ");
        manualResult.Reason.ShouldContain("public method Publisher.Add");
    }

    private static void AssertResult(PatternResult result, string? manual)
    {
        if (manual is null)
        {
            result.ShouldBeOfType<FixedResult>();
        }
        else
        {
            ManualResult manualResult = result.ShouldBeOfType<ManualResult>();
            manualResult.Reason.ShouldStartWith(AssemblyResolution.AssemblyNotStaticallyKnown + ": ");
            manualResult.Reason.ShouldContain(manual);
        }
    }

    // A Library project with the AddMediatR method and, when call is set, a Host project that references it and makes that call.
    private static async Task<PatternResult> RewriteAsync(string library, OutputKind kind, string? call)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var libraryId = ProjectId.CreateNewId();
        var registration = DocumentId.CreateNewId(libraryId);
        Solution solution = TestSolution.NewEmpty()
            .AddProject(libraryId, "Library", "Library", LanguageNames.CSharp).WithProjectCompilationOptions(libraryId, new CSharpCompilationOptions(kind))
            .AddMetadataReferences(libraryId, TestReferences.MediatR)
            .AddDocument(registration, "Registration.cs", library, filePath: "Registration.cs");
        if (call is not null)
        {
            var host = ProjectId.CreateNewId();
            solution = solution
                .AddProject(host, "Host", "Host", LanguageNames.CSharp).WithProjectCompilationOptions(host, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(host, TestReferences.MediatR)
                .AddProjectReference(host, new ProjectReference(libraryId))
                .AddDocument(DocumentId.CreateNewId(host), "Use.cs",
                    "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\nclass Use { void M(IServiceCollection services) { " + call + " } }", filePath: "Use.cs");
        }

        Document document = solution.GetDocument(registration)!;
        SemanticModel model = (await document.GetSemanticModelAsync(token))!;
        var pattern = new AddMediatRPattern();
        SyntaxNode node = pattern.Find((await document.GetSyntaxRootAsync(token))!, model).Single();
        return await pattern.RewriteAsync(node, model, solution, token);
    }
}
