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

    private static Task<FixReport> FixAsync(Solution solution) =>
        new Fixer([new MassTransitFix()]).FixAsync(solution, new FixRequest { FixId = "masstransit" }, TestContext.Current.CancellationToken);
}
