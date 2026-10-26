using System;
using System.Collections.Generic;
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
        logger.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => ((IReadOnlyList<KeyValuePair<string, object>>)(object)v).Count == 1),
            It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        var sut = logger.Object;
    }
}
