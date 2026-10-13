using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class InternalHandlerAccessPatternTests
{
    private const string Library = """
        // @project Library
        using System.Threading;
        using System.Threading.Tasks;
        using MediatR;

        namespace Library;

        public class Marker
        {
        }

        internal class Ping : INotification
        {
        }

        internal class PingHandler : INotificationHandler<Ping>
        {
            public Task Handle(Ping notification, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        """;

    private static string Host(string name) => $$"""
        // @project {{name}} -> Library
        using Microsoft.Extensions.DependencyInjection;

        namespace {{name}};

        public static class Startup
        {
            public static void Configure(IServiceCollection services) =>
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Library.Marker>());
        }

        """;

    [Fact]
    public void CompileErrors_GeneratedMediatorWithoutInternalsVisibleTo_IsCS0122()
    {
        // Arrange: the fixed code without the attribute; the generator in Host names Library's internal handler.
        string after = Library.Replace("using MediatR;", "using Mediator;", StringComparison.Ordinal)
            .Replace("public Task Handle(Ping notification, CancellationToken cancellationToken) => Task.CompletedTask;",
                "public ValueTask Handle(Ping notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;", StringComparison.Ordinal)
            + Host("Host").Replace("services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Library.Marker>());",
                "services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient);", StringComparison.Ordinal);

        // Act
        IReadOnlyList<Diagnostic> errors = TestProjects.CompileErrors(after, TestReferences.Mediator, TestReferences.MediatorGenerators);

        // Assert
        errors.ShouldContain(e => e.Id == "CS0122", string.Join(" | ", errors));
    }

    [Fact]
    public async Task FixAsync_TwoHostsScanInternalHandlers_OneAttributePerHostInOneFile()
    {
        // Arrange
        string before = Library + Host("Web") + Host("Api");
        Solution solution = TestProjects.Create(before, TestReferences.MediatR);

        // Act
        FixReport report = await new Fixer([new MediatRFix()]).FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string library = (await report.Solution.Projects.Single(p => p.Name == "Library").Documents.Single().GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        library.ShouldContain("""
            using Mediator;

            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Api")]
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Web")]

            namespace Library;
            """.ReplaceLineEndings("\n"));
        report.Findings.Count(f => f.PatternId == "mediatr.internal-handler-access").ShouldBe(1);
    }

    [Fact]
    public async Task FixAsync_LibraryAlreadyGrantsHost_AddsNoAttribute()
    {
        // Arrange
        string before = Library.Replace("namespace Library;", "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Web\")]\n\nnamespace Library;", StringComparison.Ordinal)
            + Host("Web");
        Solution solution = TestProjects.Create(before, TestReferences.MediatR);

        // Act
        FixReport report = await new Fixer([new MediatRFix()]).FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
        report.Findings.ShouldNotContain(f => f.PatternId == "mediatr.internal-handler-access");
    }

    [Fact]
    public async Task FixAsync_NoOtherProjectScansLibrary_AddsNothing()
    {
        // Arrange: Web references Library but scans only itself.
        string before = Library + Host("Web").Replace("RegisterServicesFromAssemblyContaining<Library.Marker>()", "RegisterServicesFromAssembly(typeof(Startup).Assembly)", StringComparison.Ordinal);
        Solution solution = TestProjects.Create(before, TestReferences.MediatR);

        // Act
        FixReport report = await new Fixer([new MediatRFix()]).FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldNotContain(f => f.PatternId == "mediatr.internal-handler-access");
    }
}
