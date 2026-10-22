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
        logger.Object.DidNotReceive().Log(Arg.Is<LogLevel>(LogLevel.Error), Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), Arg.Any<Exception>(),
            Arg.Any<Func<Arg.AnyType, Exception, string>>());
        var sut = logger.Object;
    }
}
