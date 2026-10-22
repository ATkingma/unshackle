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

public static class Read
{
    public static string Clock(IClock c) =>
        $"{c.Now}|{c.Count()}|{c.PingAsync().IsCompleted}|{c.TotalAsync().Result}|{c.Values().Length}|{System.Linq.Enumerable.Count(c.Items())}";
}

public static class Probe
{
    public static string Run() => Read.Clock(Mock.Of<IClock>()) + "/" + Read.Clock(new Mock<IClock>(MockBehavior.Loose).Object);
}
