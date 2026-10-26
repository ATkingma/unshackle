using System;
using Microsoft.Extensions.Logging;
using Moq;
using NSubstitute;

namespace Proofs;

public class Worker
{
}

public class LoggerProof
{
    public void Test()
    {
        var logger = new Mock<ILogger<Worker>>();
        logger.Object.Received(1).Log(Arg.Is<LogLevel>(LogLevel.Information), Arg.Any<EventId>(), Arg.Is<Arg.AnyType>((object v) => v.ToString().Contains("done")), Arg.Any<Exception>(),
            Arg.Any<Func<Arg.AnyType, Exception, string>>());
        var sut = logger.Object;
    }
}
