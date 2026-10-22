// Expect: Threw System.NullReferenceException
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

public static class Probe
{
    public static string Run()
    {
        var names = new Mock<INames>();
        return names.Object.Name(1).Length.ToString();
    }
}
