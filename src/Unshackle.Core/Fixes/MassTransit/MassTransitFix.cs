using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.MassTransit.Patterns;

namespace Unshackle.Core.Fixes.MassTransit;

/// <summary>
/// MassTransit 8 (Apache-2.0; v9 commercial): a report-only detector (TAAK-310, council round 8 T1–T5). Every feature is a Manual row with
/// guidance for OpenTransit, Wolverine and Rebus; nothing is rewritten and no package is touched.
/// </summary>
public class MassTransitFix : IFix, IReportOnlyFix
{
    public string Id => "masstransit";

    public string ReportOnlyReason =>
        "For now, pin MassTransit < 9; 9.x is commercial. No free drop-in target ships on NuGet yet (OpenTransit is unreleased); stay on 8.5.x and re-check by 2027-01-15 (council round 24, R2-R4).";

    public IReadOnlyList<string> RemovedNamespaces { get; } = [MassTransitSyntax.Namespace];

    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // Find only reads syntax and symbols; no pattern has state.
    public bool FixesFilesInParallel => true;

    public IReadOnlyList<IPattern> Patterns { get; } =
    [
        new RegistrationPattern(),
        new TransportPattern(),
        new OutboxPattern(),
        new RetryPattern(),
        new TestHarnessPattern(),
        new ConsumerPattern(),
        new ConsumerDefinitionPattern(),
        new SagaPattern(),
        new RequestClientPattern(),
        new PublishSendPattern(),
    ];
}
