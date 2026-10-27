using System;
using Xunit;

public class Logger
{
    private readonly ITestOutputHelper _output;

    public Logger(ITestOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message) => _output.WriteLine("{0}: {1}", DateTime.UtcNow.Year, message);
}
