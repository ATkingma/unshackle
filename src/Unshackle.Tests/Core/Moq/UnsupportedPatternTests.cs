using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

public class UnsupportedPatternTests
{
    [Theory]
    [InlineData("feed.Reset();", MoqReason.MockReset)]
    [InlineData("var n = feed.Invocations.Count;", MoqReason.MockInvocations)]
    [InlineData("feed.As<System.IDisposable>();", MoqReason.MockAs)]
    public async Task Fixer_UnsupportedMockUse_ManualWithOwnCodeAndCodeUntouched(string use, MoqReason code)
    {
        // Arrange
        string source = $$"""
            using Moq;
            public interface IFeed { int Next(); }
            public class T
            {
                public void Test()
                {
                    var feed = new Mock<IFeed>();
                    {{use}}
                    var sut = feed.Object;
                }
            }
            """;
        var fixer = new Fixer([new MoqFix(MoqReferences.NSubstituteAssembly)]);
        Solution solution = TestSolution.Create(source, MoqReferences.Moq);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "moq" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Select(f => f.Result).OfType<ManualResult>().ShouldContain(m => m.Code == code.ToString());
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldContain(use);
    }
}
