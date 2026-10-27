using Microsoft.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.EfBulk;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Fixes.MassTransit;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.SpecFlow;
using Unshackle.Core.Fixes.XunitV3;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Unshackle.Tests.Core.EfBulk;
using Unshackle.Tests.Core.EPPlus;
using Unshackle.Tests.Core.MassTransit;
using Unshackle.Tests.Core.Moq;
using Unshackle.Tests.Core.Polly;
using Unshackle.Tests.Core.SpecFlow;
using Unshackle.Tests.Core.XunitV3;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests;

// Runs every proof in Proofs/<Fix>/<Pattern>/<case>.before.cs through the Fixer with only that pattern.
// Expected: <case>.after.cs (exact text), <case>.manual.txt (every finding Manual, reason contains the text) or <case>.info.txt (every
// finding Info, text contains the file's content; the code stays as it is).
public class ProofTests
{
    private static readonly string s_proofsDirectory = Path.Combine(AppContext.BaseDirectory, "Proofs");

    // Proofs of known pattern bugs: kept as documentation, not run until the bug's task is done.
    private const string PendingFolderName = "_Pending";

    // The expectation files of a proof; exactly one exists.
    private static readonly string[] s_expectations = [".after.cs", ".manual.txt", ".info.txt"];

    private static readonly Dictionary<string, ProofFix> s_fixes = new(StringComparer.Ordinal)
    {
        ["FluentAssertions"] = new()
        {
            // Like the real fix: Shouldly is in the model, so a rewritten call that binds elsewhere is seen (TAAK-199).
            Fix = new FluentAssertionsFix(TestReferences.ShouldlyAssembly),
            Before = TestReferences.FluentAssertions,
            After = TestReferences.Shouldly,
        },
        ["MediatR"] = new()
        {
            Fix = new MediatRFix(),
            Before = TestReferences.MediatR,
            After = TestReferences.Mediator,
            AfterGenerators = TestReferences.MediatorGenerators,
        },
        ["AutoMapper"] = new()
        {
            Fix = new AutoMapperFix(),
            Before = TestReferences.AutoMapper,
            After = TestReferences.AutoMapper,
        },
        ["Moq"] = new()
        {
            Fix = new MoqFix(MoqReferences.NSubstituteAssembly),
            Before = MoqReferences.Moq,
            After = MoqReferences.NSubstitute,
        },
        ["Polly"] = new()
        {
            Fix = new PollyFix(),
            Before = PollyReferences.Before,
            After = PollyReferences.After,
        },
        ["EPPlus"] = new()
        {
            Fix = new EPPlusFix(),
            Before = EPPlusReferences.EPPlus,
            After = EPPlusReferences.ClosedXml,
        },
        ["EfBulk"] = new()
        {
            Fix = new EfBulkFix(),
            Before = EfBulkReferences.Before,
            After = EfBulkReferences.After,
        },
        ["SpecFlow"] = new()
        {
            Fix = new SpecFlowFix(Substitute.For<IProjectProperties>()),
            Before = SpecFlowReferences.SpecFlow,
            After = SpecFlowReferences.Reqnroll,
            AfterRunsWholeFix = true,
        },
        ["MassTransit"] = new()
        {
            // Report-only (council round 8, T1-T4): every proof is a manual.txt, "after" is never compiled.
            Fix = new MassTransitFix(),
            Before = MassTransitReferences.MassTransit,
            After = MassTransitReferences.MassTransit,
        },
        ["XunitV3"] = new()
        {
            Fix = new XunitV3Fix(),
            Before = XunitV3References.Before,
            After = XunitV3References.After,
        },
    };

    public static TheoryData<string> Proofs() =>
        [.. Directory.EnumerateFiles(s_proofsDirectory, "*.before.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(CombinedProofTests.FolderName, StringComparison.Ordinal))
            .Where(p => !p.Contains(PendingFolderName, StringComparison.Ordinal))
            .Select(p => Path.GetRelativePath(s_proofsDirectory, p)[..^".before.cs".Length].Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(Proofs))]
    public async Task Proof_Before_MatchesExpectation(string proof)
    {
        // Arrange
        string[] parts = proof.Split('/');
        s_fixes.ShouldContainKey(parts[0], $"no fix for proof folder {parts[0]} ({proof})");
        ProofFix proofFix = s_fixes[parts[0]];
        IPattern? pattern = proofFix.Fix.Patterns.SingleOrDefault(p => p.GetType().Name == parts[1] + "Pattern");
        pattern.ShouldNotBeNull($"no pattern {parts[1]}Pattern for proof {proof}");
        string basePath = Path.Combine(s_proofsDirectory, proof);
        s_expectations.Count(e => File.Exists(basePath + e))
            .ShouldBe(1, $"{proof} needs exactly one of .after.cs, .manual.txt and .info.txt");
        bool unchanged = File.Exists(basePath + ".manual.txt") || File.Exists(basePath + ".info.txt");
        string before = Normalize(await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken));
        TestSolution.CompileErrors(before, proofFix.Before).ShouldBeEmpty("before must compile against the old library");

        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns(proofFix.Fix.Id);
        fix.Patterns.Returns(proofFix.AfterRunsWholeFix && !unchanged ? proofFix.Fix.Patterns : [pattern]);
        fix.RemovedNamespaces.Returns(proofFix.Fix.RemovedNamespaces);
        fix.AddedNamespaces.Returns(proofFix.Fix.AddedNamespaces);
        // A manual proof checks that the pattern leaves the code alone; the using swap of swapped types would still run.
        fix.SwappedTypes.Returns(unchanged ? [] : proofFix.Fix.SwappedTypes);
        fix.AddedTypes.Returns(proofFix.Fix.AddedTypes);
        // Like the real fix: a rewrite that leaves the old library's calls on its result (`Should.Throw<T>(act).P.Should()`)
        // only binds with the new library in the model, and keeps the old using.
        fix.AddedReferences.Returns(proofFix.Fix.AddedReferences);
        fix.RemovedExtensionMethods.Returns(proofFix.Fix.RemovedExtensionMethods);
        var fixer = new Fixer([fix]);
        Solution solution = TestSolution.Create(before, proofFix.Before);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = fix.Id }, TestContext.Current.CancellationToken);

        // Assert
        string actual = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        report.Findings.ShouldContain(f => !proofFix.AfterRunsWholeFix || unchanged || f.PatternId == pattern.Id, "the pattern found nothing in the proof");
        if (File.Exists(basePath + ".manual.txt"))
        {
            string reason = (await File.ReadAllTextAsync(basePath + ".manual.txt", TestContext.Current.CancellationToken)).Trim();
            reason.ShouldNotBeNullOrWhiteSpace("manual.txt must name the reason");
            report.Findings.ShouldAllBe(f => f.Result is ManualResult && ReportRow.ReasonOf((ManualResult)f.Result).Contains(reason),
                string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.Result.GetType().Name)));
            actual.ShouldBe(before);
        }
        else if (File.Exists(basePath + ".info.txt"))
        {
            string text = (await File.ReadAllTextAsync(basePath + ".info.txt", TestContext.Current.CancellationToken)).Trim();
            text.ShouldNotBeNullOrWhiteSpace("info.txt must name the text");
            report.Findings.ShouldAllBe(f => f.Result is InfoResult && ((InfoResult)f.Result).Text.Contains(text),
                string.Join(" | ", report.Findings.Select(f => f.Result.ToString())));
            actual.ShouldBe(before);
        }
        else
        {
            string after = Normalize(await File.ReadAllTextAsync(basePath + ".after.cs", TestContext.Current.CancellationToken));
            report.Findings.ShouldContain(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.Result.GetType().Name)));
            actual.ShouldBe(after);
            // A Manual finding can keep the old library (e.g. inactive #if code still uses it), and so does a kept removed
            // using (a catch of AssertionFailedException that now also catches Shouldly's); then both are referenced.
            IEnumerable<MetadataReference> afterReferences = report.Findings.Any(f => f.Result is ManualResult)
                || proofFix.Fix.RemovedNamespaces.Any(ns => after.Contains($"using {ns};", StringComparison.Ordinal))
                ? proofFix.Before.Union(proofFix.After)
                : proofFix.After;
            TestSolution.CompileErrors(actual, afterReferences, proofFix.AfterGenerators).ShouldBeEmpty("after must compile against the new library");
        }
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");
}
