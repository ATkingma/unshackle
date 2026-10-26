using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Proofs;

public class Worker
{
    private readonly ILogger<Worker> _log;

    public Worker(ILogger<Worker> log)
    {
        _log = log;
    }

    public string Run(bool fail)
    {
        using (_log.BeginScope("scope"))
        {
        }

        if (fail)
        {
            _log.LogError(new InvalidOperationException("boom"), "failed {Id}", 7);
        }
        else
        {
            _log.LogInformation("done {Id}", 7);
        }

        _log.LogInformation("second");
        return _log.BeginScope("x") is null ? "null scope" : "scope";
    }
}

public static class Probe
{
    public static string Run()
    {
        var logger = new Mock<ILogger<Worker>>();
        string scope = new Worker(logger.Object).Run(false);
        logger.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("done")), It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        logger.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(2));
        logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Never());
        return scope;
    }
}
