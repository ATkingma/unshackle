using Reqnroll.Infrastructure;
using Reqnroll;

namespace Shop.Specs;

public class Logging
{
    private readonly IReqnrollOutputHelper _output;

    public Logging(IReqnrollOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message) => _output.WriteLine(message);
}
