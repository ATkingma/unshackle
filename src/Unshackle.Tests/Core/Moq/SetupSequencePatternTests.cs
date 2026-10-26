using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

public class SetupSequencePatternTests
{
    private const string Code = """
        using Moq;
        public interface IFeed { int Next(); }
        public class T
        {
            public void Test()
            {
                var feed = new Mock<IFeed>();
                feed.SetupSequence(f => f.Next()).Returns(1).Returns(2);
                var sut = feed.Object;
            }
        }
        """;

    [Fact]
    public async Task Fixer_ReturnsSequence_FixedWithExhaustionWarning()
    {
        // Arrange
        var fixer = new Fixer([new MoqFix(MoqReferences.NSubstituteAssembly)]);
        Solution solution = TestSolution.Create(Code, MoqReferences.Moq);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "moq" }, TestContext.Current.CancellationToken);

        // Assert
        Finding sequence = report.Findings.Single(f => f.PatternId == "moq.setup-sequence");
        sequence.Result.ShouldBeOfType<FixedResult>().Warning.ShouldBe(SetupSequenceSyntax.Warning);
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldContain("feed.Next().Returns(1, 2);");
    }
}
