using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.MassTransit;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.XunitV3;
using Unshackle.Core.Models;
using Unshackle.Tests.Core.EPPlus;
using Unshackle.Tests.Core.MassTransit;
using Unshackle.Tests.Core.Moq;
using Unshackle.Tests.Core.Polly;
using Unshackle.Tests.Core.XunitV3;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EndToEnd;

// End-to-end smoke tests: the REAL fix (as Cli builds it, all patterns) through the real Fixer, with the TAAK-298 pre-filter on
// (CheckEveryFile false), on a known-good proof input. ProofTests mocks the fix (one pattern, copied settings), so a fix whose
// pre-filter skips every file (an empty old-library name list) still passes there; here it must rewrite (or report) something.
public class NewFixesSmokeTests
{
    private static readonly string s_proofs = Path.Combine(AppContext.BaseDirectory, "Proofs");

    [Fact]
    public async Task Moq_RealFix_RewritesKnownGoodFile()
    {
        await ShouldRewriteAsync(new MoqFix(), "moq", "Moq/Create/field", MoqReferences.Moq);
    }

    [Fact]
    public async Task Polly_RealFix_RewritesKnownGoodFile()
    {
        await ShouldRewriteAsync(new PollyFix(), "polly", "Polly/RetryVariants/retry-count", PollyReferences.Before);
    }

    [Fact]
    public async Task XunitV3_RealFix_RewritesKnownGoodFile()
    {
        await ShouldRewriteAsync(new XunitV3Fix(), "xunit3", "XunitV3/AsyncLifetime/async-methods", XunitV3References.Before);
    }

    [Fact]
    public async Task EPPlus_RealFix_RewritesKnownGoodFile()
    {
        await ShouldRewriteAsync(new EPPlusFix(), "epplus", "EPPlus/LicenseContext/statement", EPPlusReferences.EPPlus);
    }

    [Fact]
    public async Task MassTransit_RealFix_ReportsKnownGoodFile()
    {
        // Arrange
        string before = await ReadAsync("MassTransit/Consumer/consumer");
        Solution solution = TestSolution.Create(before, MassTransitReferences.MassTransit);

        // Act
        FixReport report = await RunAsync(new MassTransitFix(), "masstransit", solution);

        // Assert: beyond the inventory-only header, the consumer is a row of its own.
        report.Findings.ShouldContain(f => f.PatternId == "masstransit.consumer" && (f.Result is ManualResult || f.Result is InfoResult),
            Describe(report));
    }

    private static async Task ShouldRewriteAsync(IFix fix, string fixId, string proof, IEnumerable<MetadataReference> references)
    {
        // Arrange
        string before = await ReadAsync(proof);
        Solution solution = TestSolution.Create(before, references);

        // Act
        FixReport report = await RunAsync(fix, fixId, solution);

        // Assert
        report.Findings.ShouldContain(f => f.Result is FixedResult, Describe(report));
        string after = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        after.ReplaceLineEndings("\n").ShouldNotBe(before, "the real fix must change the known-good file");
    }

    private static Task<FixReport> RunAsync(IFix fix, string fixId, Solution solution) =>
        new Fixer([fix]).FixAsync(solution, new FixRequest { FixId = fixId }, TestContext.Current.CancellationToken);

    private static async Task<string> ReadAsync(string proof) =>
        (await File.ReadAllTextAsync(Path.Combine(s_proofs, proof + ".before.cs"), TestContext.Current.CancellationToken)).ReplaceLineEndings("\n");

    private static string Describe(FixReport report) =>
        report.Findings.Count == 0 ? "no findings at all" : string.Join(" | ", report.Findings.Select(f => $"{f.PatternId}: {f.Result}"));
}
