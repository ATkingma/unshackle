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

public interface INames
{
    string Name(int id);
    IClock Clock { get; }
    System.Threading.Tasks.Task<string> LoadAsync(string key);
    System.Func<int> Factory();
    int Length { get; }
}

public static class Read
{
    public static string Names(INames n) =>
        $"{n.Name(1) is null}|{n.Clock is null}|{n.LoadAsync("k").Result is null}|{n.Factory() is null}|{n.Length}";
}

public static class Probe
{
    public static string Run()
    {
        var names = new Mock<INames>();
        return Read.Names(names.Object);
    }
}
