using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// TAAK-274: a MediatR solution blocked by Manual findings migrates partially; the code tied to them stays on MediatR.
public class MediatRPartialTests
{
    private const string ProjectPath = "/repo/Shop/Shop.csproj";
    private const string FilePath = "/repo/Shop/Shop.cs";

    // fullcart in small: MediatR's own Mediator registered by hand (no pattern migrates that: the root), a request side and a
    // notification side registered as an open generic.
    private const string FullCart = """
        using System.Threading;
        using System.Threading.Tasks;
        using MediatR;
        using Microsoft.Extensions.DependencyInjection;

        namespace Shop;

        public record GetBrand(int Id) : IRequest<string>;

        public class GetBrandHandler : IRequestHandler<GetBrand, string>
        {
            public Task<string> Handle(GetBrand request, CancellationToken cancellationToken) => Task.FromResult("brand " + request.Id);
        }

        public interface IDomainEvent : INotification;

        public record OrderPlaced(int Id) : IDomainEvent;

        public class OrderPlacedHandler : INotificationHandler<OrderPlaced>
        {
            public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        public class Dispatching<TNotification> : INotificationHandler<TNotification>
            where TNotification : INotification
        {
            private readonly INotificationHandler<TNotification> _inner;

            public Dispatching(INotificationHandler<TNotification> inner)
            {
                _inner = inner;
            }

            public Task Handle(TNotification notification, CancellationToken cancellationToken) => _inner.Handle(notification, cancellationToken);
        }

        public class BrandController
        {
            private readonly IMediator _mediator;

            public BrandController(IMediator mediator)
            {
                _mediator = mediator;
            }

            public Task<string> Get(int id) => _mediator.Send(new GetBrand(id));
        }

        public class Dispatcher
        {
            private readonly IMediator _publisher;

            public Dispatcher(IMediator publisher)
            {
                _publisher = publisher;
            }

            public Task Dispatch(IDomainEvent domainEvent) => _publisher.Publish(domainEvent);
        }

        public static class DependencyInjection
        {
            public static IServiceCollection AddApplication(this IServiceCollection services)
            {
                services.AddScoped<IMediator, Mediator>();
                services.AddTransient(typeof(INotificationHandler<OrderPlaced>), typeof(OrderPlacedHandler));
                services.AddTransient(typeof(INotificationHandler<>), typeof(Dispatching<>));
                return services;
            }
        }

        """;

    private const string FullCartAfter = """
        using System.Threading;
        using System.Threading.Tasks;
        using Mediator;
        using Microsoft.Extensions.DependencyInjection;

        namespace Shop;

        public record GetBrand(int Id) : IRequest<string>;

        public class GetBrandHandler : IRequestHandler<GetBrand, string>
        {
            public ValueTask<string> Handle(GetBrand request, CancellationToken cancellationToken) => ValueTask.FromResult("brand " + request.Id);
        }

        public interface IDomainEvent : global::MediatR.INotification;

        public record OrderPlaced(int Id) : IDomainEvent;

        public class OrderPlacedHandler : global::MediatR.INotificationHandler<OrderPlaced>
        {
            public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        public class Dispatching<TNotification> : global::MediatR.INotificationHandler<TNotification>
            where TNotification : global::MediatR.INotification
        {
            private readonly global::MediatR.INotificationHandler<TNotification> _inner;

            public Dispatching(global::MediatR.INotificationHandler<TNotification> inner)
            {
                _inner = inner;
            }

            public Task Handle(TNotification notification, CancellationToken cancellationToken) => _inner.Handle(notification, cancellationToken);
        }

        public class BrandController
        {
            private readonly IMediator _mediator;

            public BrandController(IMediator mediator)
            {
                _mediator = mediator;
            }

            public Task<string> Get(int id) => _mediator.Send(new GetBrand(id)).AsTask();
        }

        public class Dispatcher
        {
            private readonly global::MediatR.IMediator _publisher;

            public Dispatcher(global::MediatR.IMediator publisher)
            {
                _publisher = publisher;
            }

            public Task Dispatch(IDomainEvent domainEvent) => _publisher.Publish(domainEvent);
        }

        public static class DependencyInjection
        {
            public static IServiceCollection AddApplication(this IServiceCollection services)
            {
                services.AddScoped<global::MediatR.IMediator, global::MediatR.Mediator>();
                services.AddMediator(cfg =>
                {
                    cfg.ServiceLifetime = ServiceLifetime.Scoped;
                });
                services.AddTransient(typeof(global::MediatR.INotificationHandler<OrderPlaced>), typeof(OrderPlacedHandler));
                services.AddTransient(typeof(global::MediatR.INotificationHandler<>), typeof(Dispatching<>));
                return services;
            }
        }

        """;

    private static readonly IReadOnlyList<MetadataReference> s_both = [.. TestReferences.MediatR.Concat(TestReferences.Mediator).DistinctBy(r => r.Display)];

    private readonly IProjectFileStore _projectFiles = Substitute.For<IProjectFileStore>();

    public MediatRPartialTests()
    {
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task FixAsync_MediatRRegisteredByHand_MovesRequestsAndKeepsNotifications()
    {
        // Arrange
        TestSolution.CompileErrors(FullCart, TestReferences.MediatR).ShouldBeEmpty("before must compile against MediatR");
        var fixer = new Fixer([new MediatRFix()], _projectFiles);

        // Act
        FixReport report = await fixer.FixAsync(Create(FullCart), new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        string actual = await TextAsync(report);
        actual.ShouldBe(FullCartAfter);
        TestSolution.CompileErrors(actual, s_both, TestReferences.MediatorGenerators).ShouldBeEmpty("after must compile against MediatR and Mediator");
        report.Findings.Where(f => f.Result is FixedResult).Select(f => f.PatternId).ShouldBe(["mediatr.send-as-task", "mediatr.request-handler", "mediatr.add-mediatr"], ignoreOrder: true);
        report.Findings.ShouldContain(f => f.PatternId == "mediatr.generic-decorator" && f.Line == 24 && f.Result is ManualResult, "the root");
        // IDomainEvent, OrderPlacedHandler, the decorator (declaration, field, constructor), Dispatcher (field, constructor, Dispatch),
        // MediatR's registration and the two notification handler registrations.
        report.Findings.Where(f => f.PatternId == MediatROwners.KeptPatternId).Select(f => f.Line).ShouldBe([15, 19, 27, 29, 34, 51, 53, 58, 65, 66, 67]);
        await _projectFiles.Received(2).EditAsync(Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().KeepRemovedPackage), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_NothingCanMove_ChangesNothing()
    {
        // Arrange: one IMediator field sends a request and publishes a notification, so both sides hang on the kept registration.
        string code = FullCart.Replace(
            "public Task Dispatch(IDomainEvent domainEvent) => _publisher.Publish(domainEvent);",
            "public Task Dispatch(IDomainEvent domainEvent) => _publisher.Publish(domainEvent);\n\n    public Task<string> Brand() => _publisher.Send(new GetBrand(1));",
            StringComparison.Ordinal);
        var fixer = new Fixer([new MediatRFix()], _projectFiles);

        // Act
        FixReport report = await fixer.FixAsync(Create(code), new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
       (await TextAsync(report)).ShouldBe(code);
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
    }

    private static async Task<string> TextAsync(FixReport report) =>
        (await report.Solution.Projects.Single().Documents.Single(d => d.FilePath == FilePath).GetTextAsync(TestContext.Current.CancellationToken)).ToString();

    private static Solution Create(string code)
    {
        var projectId = ProjectId.CreateNewId();
        return new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Shop", "Shop", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(TestReferences.MediatR))
            .AddDocument(DocumentId.CreateNewId(projectId), "Shop.cs", code, filePath: FilePath);
    }
}
