public class Logger
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public Logger(global::Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message) => _output.WriteLine(message);
}
