using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.MassTransit;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.MassTransit;

// TAAK-310 step 4, council round 8 T1-T5: the MassTransit detector is report-only; every feature is one Manual row.
public class MassTransitFixTests
{
    private const string App = """
        using System;
        using System.Threading.Tasks;
        using MassTransit;
        using Microsoft.Extensions.DependencyInjection;

        public record OrderSubmitted(Guid OrderId);

        public class OrderConsumer : IConsumer<OrderSubmitted>
        {
            public Task Consume(ConsumeContext<OrderSubmitted> context) => context.RespondAsync(new OrderSubmitted(context.Message.OrderId));
        }

        public class OrderConsumerDefinition : ConsumerDefinition<OrderConsumer>
        {
        }

        public static class Bus
        {
            public static void Register(IServiceCollection services) =>
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<OrderConsumer, OrderConsumerDefinition>();
                    x.AddInMemoryInboxOutbox();
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UseMessageRetry(r => r.Immediate(3));
                        cfg.ConfigureEndpoints(context);
                    });
                });
        }
        """;

    [Fact]
    public async Task FixAsync_App_ReportsEveryFeatureAsManualAndChangesNothing()
    {
        // Arrange
        Solution solution = TestSolution.Create(App, MassTransitReferences.MassTransit);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.InventoryOnly.ShouldBeTrue();
        report.Solution.ShouldBeSameAs(solution);
        report.ProjectFileChanges.ShouldBeEmpty();
        report.Findings[0].PatternId.ShouldBe(Fixer.InventoryOnlyPatternId);
        report.Findings.Skip(1).ShouldAllBe(f => f.Result is ManualResult);
        report.Findings.Skip(1).Select(f => f.PatternId).Distinct().Order(StringComparer.Ordinal).ShouldBe(
        [
            "masstransit.consumer",
            "masstransit.consumer-definition",
            "masstransit.outbox",
            "masstransit.registration",
            "masstransit.request-client",
            "masstransit.retry",
            "masstransit.transport",
        ]);
    }

    [Fact]
    public async Task FixAsync_ReasonGivesTargetGuidance()
    {
        // Act
        FixReport report = await FixAsync(TestSolution.Create(App, MassTransitReferences.MassTransit));

        // Assert
        string reason = ((ManualResult)report.Findings.First(f => f.PatternId == "masstransit.consumer").Result).Reason;
        reason.ShouldContain("Rebus IHandleMessages<T>");
        reason.ShouldContain("OpenTransit");
    }

    [Fact]
    public async Task FixAsync_UnresolvedTransportInMassTransitFile_IsMatchedByName()
    {
        // Arrange: MassTransit.RabbitMQ is not referenced, so UsingRabbitMq binds to nothing.
        const string code = """
            using MassTransit;
            using Microsoft.Extensions.DependencyInjection;

            public static class Bus
            {
                public static void Register(IServiceCollection services) =>
                    services.AddMassTransit(x => x.UsingRabbitMq((context, cfg) => cfg.Host("localhost")));
            }
            """;

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        Finding transport = report.Findings.Where(f => f.PatternId == "masstransit.transport").ShouldHaveSingleItem();
        transport.Line.ShouldBe(7);
    }

    [Fact]
    public async Task FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported()
    {
        // Arrange
        const string code = """
            using System.Threading.Tasks;
            using MassTransit;

            namespace Own
            {
                public interface IConsumer<T> { Task Consume(T message); }

                public class Handler : IConsumer<int>
                {
                    public Task Consume(int message) => Task.CompletedTask;
                }

                public class Uses : IConsumer<string>
                {
                    public Task Consume(string message) => Task.CompletedTask;

                    public static IBus? Bus { get; set; }
                }
            }
            """;

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        report.Findings.ShouldNotContain(f => f.PatternId == "masstransit.consumer");
    }

    [Fact]
    public async Task FixAsync_SagaAndTestHarness_AreReported()
    {
        // Arrange
        const string code = """
            using System;
            using MassTransit;
            using Microsoft.Extensions.DependencyInjection;

            public class OrderState : SagaStateMachineInstance
            {
                public Guid CorrelationId { get; set; }

                public string CurrentState { get; set; } = string.Empty;
            }

            public class OrderStateMachine : MassTransitStateMachine<OrderState>
            {
            }

            public static class Tests
            {
                public static void Register(IServiceCollection services) => services.AddMassTransitTestHarness();
            }
            """;

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        report.Findings.ShouldContain(f => f.PatternId == "masstransit.saga" && f.Line == 12);
        report.Findings.ShouldContain(f => f.PatternId == "masstransit.test-harness");
        report.Findings.ShouldNotContain(f => f.PatternId == "masstransit.registration");
    }

    // Council round 10, C1: every detector row carries a typed code.
    [Fact]
    public async Task FixAsync_App_EveryManualRowHasATypedCode()
    {
        // Act
        FixReport report = await FixAsync(TestSolution.Create(App, MassTransitReferences.MassTransit));

        // Assert
        List<ManualResult> manual = [.. report.Findings.Select(f => f.Result).OfType<ManualResult>()];
        manual.ShouldAllBe(m => m.Code != null);
        manual.Select(m => m.Code).ShouldContain("BusRegistration");
        manual.Select(m => m.Code).ShouldContain("Transport");
        manual.Select(m => m.Code).ShouldContain("RetryPolicy");
    }

    // Council round 11, T7: a consumer using only context.Message and cancellation is an Info row.
    [Theory]
    [InlineData("Task.CompletedTask")]
    [InlineData("Handle(context.Message.OrderId, context.CancellationToken)")]
    public async Task FixAsync_SimpleConsumer_IsInfo(string body)
    {
        // Arrange
        string code = Consumer(body);

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        Finding consumer = report.Findings.Where(f => f.PatternId == "masstransit.consumer").ShouldHaveSingleItem();
        consumer.Result.ShouldBeOfType<InfoResult>(consumer.Result.ToString()).Text.ShouldContain("Simple MassTransit consumer of OrderSubmitted");
    }

    [Theory]
    [InlineData("context.Headers.Get<string>(\"x\") is null ? Task.CompletedTask : Task.CompletedTask", "ConsumerHeaders")]
    [InlineData("context.RespondAsync(context.Message)", "ConsumerRespond")]
    [InlineData("context.Redeliver(TimeSpan.FromSeconds(1))", "ConsumerRedeliver")]
    [InlineData("context.Publish(context.Message)", "ConsumerPublish")]
    [InlineData("Pass(context)", "ConsumerContextFeature")]
    [InlineData("Task.FromResult(context.MessageId)", "ConsumerContextFeature")]
    public async Task FixAsync_ConsumerWithFeature_IsManualWithFeatureCode(string body, string code)
    {
        // Act
        FixReport report = await FixAsync(TestSolution.Create(Consumer(body), MassTransitReferences.MassTransit));

        // Assert
        Finding consumer = report.Findings.Where(f => f.PatternId == "masstransit.consumer").ShouldHaveSingleItem();
        ((ManualResult)consumer.Result).Code.ShouldBe(code);
    }

    [Fact]
    public async Task FixAsync_ConsumerWithInjectedPublishEndpoint_IsManualPublish()
    {
        // Arrange
        string code = Consumer("_publish.Publish(context.Message)").Replace(
            "public class OrderConsumer : IConsumer<OrderSubmitted>\n{",
            "public class OrderConsumer(IPublishEndpoint _publish) : IConsumer<OrderSubmitted>\n{", StringComparison.Ordinal);

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        ((ManualResult)report.Findings.Single(f => f.PatternId == "masstransit.consumer").Result).Code.ShouldBe("ConsumerPublish");
    }

    [Theory]
    [InlineData("public class OrderConsumerDefinition : ConsumerDefinition<OrderConsumer> { }", "ConsumerWithDefinition")]
    [InlineData("public static class Bus { public static void Register(IBusFactoryConfigurator cfg) => cfg.UseConsumeFilter(typeof(object), null!); }", "ConsumerFilter")]
    public async Task FixAsync_DefinitionOrFilterElsewhere_KeepsSimpleConsumerManual(string other, string code)
    {
        // Arrange
        Solution solution = TestSolution.Create(Consumer("Task.CompletedTask") + "\n" + other, MassTransitReferences.MassTransit);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        ((ManualResult)report.Findings.Single(f => f.PatternId == "masstransit.consumer").Result).Code.ShouldBe(code);
    }

    [Fact]
    public async Task FixAsync_FaultConsumer_IsManualSpecialMessage()
    {
        // Arrange
        string code = Consumer("Task.CompletedTask").Replace("IConsumer<OrderSubmitted>", "IConsumer<Fault<OrderSubmitted>>", StringComparison.Ordinal)
            .Replace("ConsumeContext<OrderSubmitted>", "ConsumeContext<Fault<OrderSubmitted>>", StringComparison.Ordinal);

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        ((ManualResult)report.Findings.Single(f => f.PatternId == "masstransit.consumer").Result).Code.ShouldBe("ConsumerSpecialMessage");
    }

    // Council round 11, T6: Publish/Send are Info rows only when the symbol resolves to a listed MassTransit type.
    [Fact]
    public async Task FixAsync_PublishAndSendOnMassTransitTypes_AreInfo()
    {
        // Arrange
        const string code = """
            using System;
            using System.Threading.Tasks;
            using MassTransit;

            public record OrderSubmitted(Guid OrderId);

            public class Orders(IPublishEndpoint publish, ISendEndpointProvider provider, IBus bus)
            {
                public async Task Submit(OrderSubmitted order)
                {
                    await publish.Publish(order);
                    ISendEndpoint endpoint = await provider.GetSendEndpoint(new Uri("queue:orders"));
                    await endpoint.Send(order);
                    await bus.Publish(order);
                }
            }
            """;

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        List<Finding> rows = [.. report.Findings.Where(f => f.PatternId == "masstransit.publish-send")];
        rows.Select(f => f.Line).ShouldBe([11, 13, 14]);
        rows.ShouldAllBe(f => f.Result is InfoResult);
        ((InfoResult)rows[1].Result).Text.ShouldContain("MassTransit Send on ISendEndpoint");
    }

    [Fact]
    public async Task FixAsync_PublishOnOwnOrUnresolvedType_IsNotReported()
    {
        // Arrange: a name-only match (own type, unresolved receiver) is never reported.
        const string code = """
            using System.Threading.Tasks;
            using MassTransit;

            public interface IPublisher { Task Publish(object message); }

            public class Orders(IPublisher publisher)
            {
                public Task Submit(object order) => publisher.Publish(order);

                public Task Other(object order) => Unknown.Send(order);
            }
            """;

        // Act
        FixReport report = await FixAsync(TestSolution.Create(code, MassTransitReferences.MassTransit));

        // Assert
        report.Findings.ShouldNotContain(f => f.PatternId == "masstransit.publish-send");
    }

    private static string Consumer(string body) => $$"""
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using MassTransit;

        public record OrderSubmitted(Guid OrderId);

        public class OrderConsumer : IConsumer<OrderSubmitted>
        {
            public Task Consume(ConsumeContext<OrderSubmitted> context) => {{body}};

            private static Task Handle(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

            private static Task Pass(object context) => Task.CompletedTask;
        }
        """;

    private static Task<FixReport> FixAsync(Solution solution) =>
        new Fixer([new MassTransitFix()]).FixAsync(solution, new FixRequest { FixId = "masstransit" }, TestContext.Current.CancellationToken);
}
