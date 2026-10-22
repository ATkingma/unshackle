using Moq;

namespace Proofs;

public interface IClock
{
    System.DateTime Now { get; }
    int Count();
    System.Threading.Tasks.Task PingAsync();
    System.Threading.Tasks.Task<int> TotalAsync();
    int[] Values();
    System.Collections.Generic.IEnumerable<int> Items();
}

public static class Probe
{
    public static string Run()
    {
        var unused = new Mock<IClock>();
        return "ran";
    }
}
