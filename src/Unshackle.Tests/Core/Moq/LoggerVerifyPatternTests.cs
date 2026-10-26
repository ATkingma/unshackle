using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

public class LoggerVerifyPatternTests
{
    private const string Code = """
        using System;
        using Microsoft.Extensions.Logging;
        using Moq;
        public class T
        {
            public void Test()
            {
                var logger = new Mock<ILogger<T>>();
                logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Never());
                var sut = logger.Object;
            }
        }
        """;

    [Fact]
    public async Task Fixer_LogVerify_UsesLoggerVerifyPatternAndArgAnyType()
    {
        // Arrange
        var fixer = new Fixer([new MoqFix(MoqReferences.NSubstituteAssembly)]);
        Solution solution = TestSolution.Create(Code, MoqReferences.Moq);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "moq" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Single(f => f.PatternId == "moq.logger-verify").Result.ShouldBeOfType<FixedResult>();
        report.Findings.ShouldNotContain(f => f.PatternId == "moq.verify");
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldContain("DidNotReceive().Log(Arg.Is<LogLevel>(LogLevel.Error)");
    }
}
