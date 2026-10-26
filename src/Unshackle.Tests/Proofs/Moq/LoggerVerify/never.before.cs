using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Proofs;

public class Worker
{
}

public class LoggerProof
{
    public void Test()
    {
        var logger = new Mock<ILogger<Worker>>();
        logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never());
        var sut = logger.Object;
    }
}
