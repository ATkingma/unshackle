using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class MediatorHostsTests
{
    private static readonly string s_proof = Path.Combine(AppContext.BaseDirectory, "Proofs", "MediatR", "_Combined", "host-references-host.before.cs");

    [Fact]
    public async Task AssemblyNamesAsync_TwoAutofacModules_FindsBothHosts()
    {
        // Arrange
        string code = (await File.ReadAllTextAsync(s_proof, TestContext.Current.CancellationToken)).ReplaceLineEndings("\n");
        Solution solution = TestProjects.Create(code, TestReferences.MediatR);

        // Act
        IReadOnlySet<string> hosts = await MediatorHosts.AssemblyNamesAsync(solution, TestContext.Current.CancellationToken);

        // Assert
        hosts.ShouldBe(["MyMeetings.Modules.Registrations.Infrastructure", "MyMeetings.Modules.UserAccess.Infrastructure"], ignoreOrder: true);
    }

    [Fact]
    public async Task SettingsAsync_ReferencedHost_GeneratesTypesAsInternal()
    {
        // Arrange
        Solution solution = await CreateAsync(friend: false);

        // Act
        MediatorHostSettings settings = await SettingsAsync(solution, "MyMeetings.Modules.UserAccess.Infrastructure");

        // Assert
        settings.ManualReason.ShouldBeNull();
        settings.Assignments.ShouldBe([("GenerateTypesAsInternal", "true")]);
    }

    [Fact]
    public async Task SettingsAsync_ReferencingHost_ListsAssembliesWithoutOtherHost()
    {
        // Arrange
        Solution solution = await CreateAsync(friend: false);

        // Act
        MediatorHostSettings settings = await SettingsAsync(solution, "MyMeetings.Modules.Registrations.Infrastructure");

        // Assert
        settings.Assignments.ShouldBe([("Assemblies",
            "[typeof(global::MyMeetings.Modules.Registrations.Infrastructure.RegistrationsMediatorModule), typeof(global::MyMeetings.Modules.Registrations.Application.ConfirmUserRegistrationCommand)]")]);
    }

    [Fact]
    public async Task SettingsAsync_HostGrantsInternalsVisibleToOtherHost_IsManual()
    {
        // Arrange: generated internals of one host visible in the other collide whatever the options (CS0436, CS0121; probe TAAK-213).
        Solution solution = await CreateAsync(friend: true);

        // Act
        MediatorHostSettings settings = await SettingsAsync(solution, "MyMeetings.Modules.UserAccess.Infrastructure");

        // Assert
        settings.ManualReason.ShouldNotBeNull().ShouldContain("InternalsVisibleTo to MyMeetings.Modules.Registrations.Infrastructure");
    }

    private static string AddMediatRHost(string name, string references, string scanned) => $$"""
        // @project {{name}}{{references}}
        using System.Threading;
        using System.Threading.Tasks;
        using MediatR;
        using Microsoft.Extensions.DependencyInjection;

        namespace {{name}};

        public class Ping{{name}} : IRequest
        {
        }

        public class Ping{{name}}Handler : IRequestHandler<Ping{{name}}>
        {
            public Task Handle(Ping{{name}} request, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        public static class Startup{{name}}
        {
            public static void Configure(IServiceCollection services) =>
                services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblyContaining<{{scanned}}>(); });
        }

        """;

    [Fact]
    public async Task FixAsync_AddMediatRHostReferencesHost_InternalTypesAndAssemblies()
    {
        // Arrange
        string before = AddMediatRHost("Library", "", "PingLibrary") + AddMediatRHost("Api", " -> Library", "PingApi");
        Solution solution = TestProjects.Create(before, TestReferences.MediatR);

        // Act
        FixReport report = await new Fixer([new MediatRFix()]).FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string after = await TestProjects.GetTextAsync(report.Solution, before, TestContext.Current.CancellationToken);
        after.ShouldContain("cfg.GenerateTypesAsInternal = true;");
        after.ShouldContain("cfg.Assemblies = [typeof(global::Api.StartupApi)];");
        TestProjects.CompileErrors(after, TestReferences.Mediator, TestReferences.MediatorGenerators).ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_AddMediatRScansOtherHost_IsManual()
    {
        // Arrange: Api scans Library, which calls AddMediatR itself: both would generate Library's handlers.
        string before = AddMediatRHost("Library", "", "PingLibrary") + AddMediatRHost("Api", " -> Library", "Library.PingLibrary");
        Solution solution = TestProjects.Create(before, TestReferences.MediatR);

        // Act
        FixReport report = await new Fixer([new MediatRFix()]).FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("AddMediatR scans Library", StringComparison.Ordinal));
    }

    private static async Task<Solution> CreateAsync(bool friend)
    {
        string code = (await File.ReadAllTextAsync(s_proof, TestContext.Current.CancellationToken)).ReplaceLineEndings("\n");
        if (friend)
        {
            code = code.Replace(
                "namespace MyMeetings.Modules.UserAccess.Infrastructure;",
                "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"MyMeetings.Modules.Registrations.Infrastructure\")]\n\nnamespace MyMeetings.Modules.UserAccess.Infrastructure;",
                StringComparison.Ordinal);
        }

        return TestProjects.Create(code, TestReferences.MediatR);
    }

    private static async Task<MediatorHostSettings> SettingsAsync(Solution solution, string project)
    {
        Document document = solution.Projects.Single(p => p.Name == project).Documents.Single();
        SyntaxNode root = (await document.GetSyntaxRootAsync(TestContext.Current.CancellationToken))!;
        SemanticModel model = (await document.GetSemanticModelAsync(TestContext.Current.CancellationToken))!;
        SyntaxNode scan = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax>().First();
        return await MediatorHosts.SettingsAsync(scan, model, solution, TestContext.Current.CancellationToken);
    }
}
