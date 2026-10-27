using TechTalk.SpecFlow.Infrastructure;

namespace Shop.Specs;

public class Logging
{
    private readonly ISpecFlowOutputHelper _output;

    public Logging(ISpecFlowOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message) => _output.WriteLine(message);
}
