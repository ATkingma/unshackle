// Expect: VerifyFailed
using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Proofs;

public class Worker
{
}

public static class Probe
{
    public static string Run()
    {
        var logger = new Mock<ILogger<Worker>>();
        logger.Object.LogInformation("only once");
        logger.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("missing")), It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        return "verified";
    }
}
