using Microsoft.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests;

// Runs every proof in Proofs/<Fix>/<Pattern>/<case>.before.cs through the Fixer with only that pattern.
// Expected: <case>.after.cs (exact text) or <case>.manual.txt (every finding Manual, reason contains the text).
public class ProofTests
{
    private static readonly string s_proofsDirectory = Path.Combine(AppContext.BaseDirectory, "Proofs");

    private static readonly Dictionary<string, ProofFix> s_fixes = new(StringComparer.Ordinal)
    {
        ["FluentAssertions"] = new()
        {
            Fix = new FluentAssertionsFix(),
            Before = TestReferences.FluentAssertions,
            After = TestReferences.Shouldly,
        },
    };

    public static TheoryData<string> Proofs() =>
        [.. Directory.EnumerateFiles(s_proofsDirectory, "*.before.cs", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(s_proofsDirectory, p)[..^".before.cs".Length].Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(Proofs))]
    public async Task Proof_Before_MatchesExpectation(string proof)
    {
        // Arrange
        string[] parts = proof.Split('/');
        ProofFix proofFix = s_fixes[parts[0]];
        IPattern pattern = proofFix.Fix.Patterns.Single(p => p.GetType().Name == parts[1] + "Pattern");
        string basePath = Path.Combine(s_proofsDirectory, proof);
        string before = Normalize(await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken));
        TestSolution.CompileErrors(before, proofFix.Before).ShouldBeEmpty("before must compile against the old library");

        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns(proofFix.Fix.Id);
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(proofFix.Fix.RemovedNamespaces);
        fix.AddedNamespaces.Returns(proofFix.Fix.AddedNamespaces);
        var fixer = new Fixer([fix]);
        Solution solution = TestSolution.Create(before, proofFix.Before);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = fix.Id }, TestContext.Current.CancellationToken);

        // Assert
        string actual = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        report.Findings.ShouldNotBeEmpty("the pattern found nothing in the proof");
        if (File.Exists(basePath + ".manual.txt"))
        {
            string reason = (await File.ReadAllTextAsync(basePath + ".manual.txt", TestContext.Current.CancellationToken)).Trim();
            report.Findings.ShouldAllBe(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains(reason));
            actual.ShouldBe(before);
        }
        else
        {
            string after = Normalize(await File.ReadAllTextAsync(basePath + ".after.cs", TestContext.Current.CancellationToken));
            report.Findings.ShouldContain(f => f.Result is FixedResult);
            actual.ShouldBe(after);
            TestSolution.CompileErrors(actual, proofFix.After).ShouldBeEmpty("after must compile against the new library");
        }
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");
}
