public class Logger
{
    private readonly Xunit.ITestOutputHelper _output;

    public Logger(global::Xunit.ITestOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message) => _output.WriteLine(message);
}
