using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

// Council round 13, M8: a finding reports under the moq-queue.md row it belongs to, not the pattern that met it.
public class SubPatternIdTests
{
    private static async Task<FixReport> FixAsync(string code)
    {
        var fixer = new Fixer([new MoqFix(MoqReferences.NSubstituteAssembly)]);
        Solution solution = TestSolution.Create(code, MoqReferences.Moq);
        return await fixer.FixAsync(solution, new FixRequest { FixId = "moq" }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Fixer_SetupProperty_ReportsAsProperties()
    {
        // Arrange
        const string code = """
            using Moq;
            public interface IBox { int Size { get; set; } }
            public class T
            {
                public void Test()
                {
                    var box = new Mock<IBox>();
                    box.SetupProperty(b => b.Size);
                    var sut = box.Object;
                }
            }
            """;

        // Act
        FixReport report = await FixAsync(code);

        // Assert
        Finding tracking = report.Findings.Single(f => f.Line == 8);
        tracking.PatternId.ShouldBe("moq.properties");
        tracking.Result.ShouldBeOfType<FixedResult>();
    }

    [Fact]
    public async Task Fixer_MockOfPredicate_ReportsAsSetupReturns()
    {
        // Arrange
        const string code = """
            using Moq;
            public interface IBox { int Size { get; } }
            public class T
            {
                public void Test()
                {
                    IBox box = Mock.Of<IBox>(b => b.Size == 1);
                }
            }
            """;

        // Act
        FixReport report = await FixAsync(code);

        // Assert
        Finding finding = report.Findings.Single(f => f.Result is ManualResult { Code: nameof(MoqReason.MockOfSetup) });
        finding.PatternId.ShouldBe("moq.setup-returns");
    }

    [Fact]
    public async Task Fixer_StrictMock_ReportsAsUnsupported()
    {
        // Arrange
        const string code = """
            using Moq;
            public interface IBox { int Size(); }
            public class T
            {
                public void Test()
                {
                    var box = new Mock<IBox>(MockBehavior.Strict);
                    var sut = box.Object;
                }
            }
            """;

        // Act
        FixReport report = await FixAsync(code);

        // Assert
        Finding[] strict = [.. report.Findings.Where(f => f.Result is ManualResult { Code: nameof(MoqReason.StrictBehaviour) })];
        strict.ShouldNotBeEmpty();
        strict.Select(f => f.PatternId).Distinct().ShouldBe(["moq.unsupported"]);
    }

    [Fact]
    public void ToResult_SetupsOverlap_NamesConfigureAsManualRoute()
    {
        // Arrange
        var blocker = MoqBlocker.Of(MoqReason.SetupsOverlap, "two setups");

        // Act
        PatternResult result = blocker.ToResult();

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldBe("two setups" + MoqBlocker.ConfigureRoute);
        result.PatternId.ShouldBeNull();
    }
}
