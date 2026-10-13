using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// TAAK-174: `using Shouldly;` next to an own ShouldBe extension (Akka.TestKit) makes calls ambiguous (CS0121).
public class AmbiguousAddedNamespaceTests
{
    private const string OwnExtension = """
        namespace TestKit
        {
            public static class SpecExtensions
            {
                public static void ShouldBe<T>(this T actual, T expected, string message = null) { }
            }
        }

        """;

    [Fact]
    public async Task FixAsync_OwnShouldBeExtensionInScope_LeavesFileAndReportsManual()
    {
        // Arrange
        string code = "using FluentAssertions;\nusing TestKit;\n\n" + OwnExtension + """
            public class C
            {
                public void T(int x)
                {
                    x.Should().Be(1);
                    x.ShouldBe(1);
                }
            }
            """;
        var fixer = new Fixer([new FluentAssertionsFix(TestReferences.ShouldlyAssembly)]);

        // Act
        FixReport report = await fixer.FixAsync(
            TestSolution.Create(code, TestReferences.FluentAssertions), new FixRequest { FixId = "fluentassertions" }, TestContext.Current.CancellationToken);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(code);
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
        report.Findings.Select(f => f.Result).OfType<ManualResult>()
            .ShouldContain(m => m.Reason.Contains("TestKit.SpecExtensions.ShouldBe", StringComparison.Ordinal) && m.Reason.Contains("CS0121", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FixAsync_OwnExtensionWithOtherName_Migrates()
    {
        // Arrange
        string code = "using FluentAssertions;\nusing TestKit;\n\n" + OwnExtension.Replace("ShouldBe<T>", "ShouldEqual<T>", StringComparison.Ordinal) + """
            public class C
            {
                public void T(int x)
                {
                    x.Should().Be(1);
                    x.ShouldEqual(1);
                }
            }
            """;
        var fixer = new Fixer([new FluentAssertionsFix(TestReferences.ShouldlyAssembly)]);

        // Act
        FixReport report = await fixer.FixAsync(
            TestSolution.Create(code, TestReferences.FluentAssertions), new FixRequest { FixId = "fluentassertions" }, TestContext.Current.CancellationToken);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldContain("x.ShouldBe(1);");
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
    }

    // TAAK-199: in the extension's own namespace it wins over `using Shouldly;` without ambiguity, so the rewrite would bind to it.
    [Fact]
    public async Task FixAsync_FileInNamespaceOfOwnShouldBe_LeavesFileAndReportsManual()
    {
        // Arrange
        string code = "using FluentAssertions;\n\n" + OwnExtension + """
            namespace TestKit
            {
                public class C
                {
                    public void T(int x)
                    {
                        x.Should().Be(1, "because {0}", "reason");
                    }
                }
            }
            """;
        var fixer = new Fixer([new FluentAssertionsFix(TestReferences.ShouldlyAssembly)]);

        // Act
        FixReport report = await fixer.FixAsync(
            TestSolution.Create(code, TestReferences.FluentAssertions), new FixRequest { FixId = "fluentassertions" }, TestContext.Current.CancellationToken);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(code);
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
        report.Findings.Select(f => f.Result).OfType<ManualResult>()
            .ShouldContain(m => m.Reason.Contains("binds to TestKit.SpecExtensions.ShouldBe instead of Shouldly", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FixAsync_FileInNamespaceOfOwnShouldBeAlreadyUsingIt_MigratesOtherCalls()
    {
        // Arrange
        string code = "using FluentAssertions;\n\n" + OwnExtension + """
            namespace TestKit
            {
                public class C
                {
                    public void T(int x, bool b)
                    {
                        x.ShouldBe(1);
                        b.Should().BeTrue();
                    }
                }
            }
            """;
        var fixer = new Fixer([new FluentAssertionsFix(TestReferences.ShouldlyAssembly)]);

        // Act
        FixReport report = await fixer.FixAsync(
            TestSolution.Create(code, TestReferences.FluentAssertions), new FixRequest { FixId = "fluentassertions" }, TestContext.Current.CancellationToken);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldContain("b.ShouldBeTrue();");
        text.ShouldContain("x.ShouldBe(1);");
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
    }
}
