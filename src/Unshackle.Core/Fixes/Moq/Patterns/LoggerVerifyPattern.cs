namespace Unshackle.Core.Fixes.Moq.Patterns;

// moq-queue.md row 12: `logger.Verify(l => l.Log(level, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("x")), It.IsAny<Exception>(),
// (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once)` → `logger.Received(1).Log(level, Arg.Any<EventId>(), Arg.Is<Arg.AnyType>((object v) => ..),
// Arg.Any<Exception>(), Arg.Any<Func<Arg.AnyType, Exception, string>>())`. Measured equal (pass and fail) with NSubstitute 6.2.0's Arg.AnyType. The
// rewrite is moq.verify's (same analysis, Times mapping and matchers); only the id and the Verify calls it finds differ. An unconfigured ILogger
// answers BeginScope with a substitute where Moq answers null: moq.create pins that with ReturnsNull.
public class LoggerVerifyPattern : VerifyPattern
{
    public override string Id => "moq.logger-verify";

    public override string Description => "logger.Verify(l => l.Log(.., It.IsAnyType ..), Times) → logger.Received(n).Log(.., Arg.AnyType ..)";

    protected override bool Logs => true;
}
