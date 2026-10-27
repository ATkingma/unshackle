using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Fixes.MassTransit;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.SpecFlow;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Unshackle.Tests.Core.EPPlus;
using Unshackle.Tests.Core.MassTransit;
using Unshackle.Tests.Core.Moq;
using Unshackle.Tests.Core.Polly;
using Unshackle.Tests.Core.SpecFlow;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Engine;

// Council round 12, G9 (round 7, D5): the pre-filter only saves time. Every fix, with all its patterns, gives the same text and the same
// rows on every proof with the pre-filter on and off (FixRequest.CheckEveryFile).
public class PreFilterContractTests
{
    private static readonly string s_proofsDirectory = Path.Combine(AppContext.BaseDirectory, "Proofs");

    private static readonly Dictionary<string, (Func<IFix> Fix, IEnumerable<MetadataReference> Before)> s_fixes = new(StringComparer.Ordinal)
    {
        ["FluentAssertions"] = (() => new FluentAssertionsFix(TestReferences.ShouldlyAssembly), TestReferences.FluentAssertions),
        ["MediatR"] = (() => new MediatRFix(), TestReferences.MediatR),
        ["AutoMapper"] = (() => new AutoMapperFix(), TestReferences.AutoMapper),
        ["Moq"] = (() => new MoqFix(MoqReferences.NSubstituteAssembly), MoqReferences.Moq),
        ["Polly"] = (() => new PollyFix(), PollyReferences.Before),
        ["EPPlus"] = (() => new EPPlusFix(), EPPlusReferences.EPPlus),
        ["SpecFlow"] = (() => new SpecFlowFix(), SpecFlowReferences.SpecFlow),
        ["MassTransit"] = (() => new MassTransitFix(), MassTransitReferences.MassTransit),
    };

    public static TheoryData<string> Proofs() =>
        [.. Directory.EnumerateFiles(s_proofsDirectory, "*.before.cs", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(s_proofsDirectory, p)[..^".before.cs".Length].Replace('\\', '/'))
            .Where(p => s_fixes.ContainsKey(p.Split('/')[0]))
            .Order(StringComparer.Ordinal)];

    [Fact]
    public void Proofs_EveryProofFolder_HasAFixHere()
    {
        // Act
        List<string> folders = [.. Directory.EnumerateDirectories(s_proofsDirectory).Select(Path.GetFileName).OfType<string>()];

        // Assert
        folders.ShouldAllBe(f => s_fixes.ContainsKey(f), "a new fix needs its entry in PreFilterContractTests too");
    }

    [Theory]
    [MemberData(nameof(Proofs))]
    public async Task FixAsync_PreFilterOnAndOff_GivesTheSameOutput(string proof)
    {
        // Arrange
        CancellationToken token = TestContext.Current.CancellationToken;
        (Func<IFix> create, IEnumerable<MetadataReference> references) = s_fixes[proof.Split('/')[0]];
        string before = (await File.ReadAllTextAsync(Path.Combine(s_proofsDirectory, proof + ".before.cs"), token)).ReplaceLineEndings("\n");

        // Act
        (string Text, List<string> Rows) on = await RunAsync(create(), before, references, checkEveryFile: false, token);
        (string Text, List<string> Rows) off = await RunAsync(create(), before, references, checkEveryFile: true, token);

        // Assert
        on.Text.ShouldBe(off.Text);
        on.Rows.ShouldBe(off.Rows);
    }

    private static async Task<(string Text, List<string> Rows)> RunAsync(
        IFix fix, string code, IEnumerable<MetadataReference> references, bool checkEveryFile, CancellationToken cancellationToken)
    {
        FixReport report = await new Fixer([fix]).FixAsync(
            TestSolution.Create(code, references), new FixRequest { FixId = fix.Id, CheckEveryFile = checkEveryFile }, cancellationToken);
        return (await TestSolution.GetTextAsync(report.Solution, cancellationToken),
            [.. ReportRow.FromAll(report.Findings).Select(r => $"{r.Line} {r.Pattern} {r.Status} {r.Reason}")]);
    }
}
