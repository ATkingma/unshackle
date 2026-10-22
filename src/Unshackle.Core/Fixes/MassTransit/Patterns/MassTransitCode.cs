namespace Unshackle.Core.Fixes.MassTransit.Patterns;

/// <summary>
/// The typed reason codes of the MassTransit detector's Manual rows (council round 10, C1; round 11, T7: one code per consumer feature).
/// </summary>
public enum MassTransitCode
{
    /// <summary>AddMassTransit / bus registration.</summary>
    BusRegistration,

    /// <summary>A transport (UsingRabbitMq, UsingInMemory, ...).</summary>
    Transport,

    /// <summary>The inbox/outbox.</summary>
    Outbox,

    /// <summary>A retry or redelivery policy on the bus or an endpoint.</summary>
    RetryPolicy,

    /// <summary>The MassTransit test harness.</summary>
    TestHarness,

    /// <summary>A ConsumerDefinition class.</summary>
    ConsumerDefinition,

    /// <summary>A saga or state machine.</summary>
    Saga,

    /// <summary>Request/response (IRequestClient, RespondAsync).</summary>
    RequestResponse,

    /// <summary>T7: a consumer that reads message headers (context.Headers, Get/TryGetHeader).</summary>
    ConsumerHeaders,

    /// <summary>T7: a consumer that responds (context.Respond / RespondAsync).</summary>
    ConsumerRespond,

    /// <summary>T7: a consumer that redelivers or defers (context.Redeliver, Defer).</summary>
    ConsumerRedeliver,

    /// <summary>T7: a consumer that publishes or sends (context, IPublishEndpoint, ISendEndpointProvider, IBus).</summary>
    ConsumerPublish,

    /// <summary>T7: a consumer with a ConsumerDefinition in the same compilation.</summary>
    ConsumerWithDefinition,

    /// <summary>T7: the compilation registers a filter (UseFilter, UseConsumeFilter, an IFilter&lt;T&gt; class), which may run around any consumer.</summary>
    ConsumerFilter,

    /// <summary>T7: a consumer of Batch&lt;T&gt; or Fault&lt;T&gt;, whose message shape is MassTransit's own.</summary>
    ConsumerSpecialMessage,

    /// <summary>T7: a consumer that uses another member of its ConsumeContext, or hands the context on.</summary>
    ConsumerContextFeature,

    /// <summary>T7: a consumer whose Consume method cannot be read (no body in this compilation, or the context parameter is missing).</summary>
    ConsumerNotAnalyzable,
}
