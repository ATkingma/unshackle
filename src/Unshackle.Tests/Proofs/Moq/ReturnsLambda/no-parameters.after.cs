using Moq;
using NSubstitute;

namespace Proofs;

public interface IRepo
{
    System.DateTime Now { get; }
    int Count(string name);
    string Find(int id, string name);
    System.Threading.Tasks.Task<int> TotalAsync(int year);
}

public class SetupProof
{
    private int _counter;

    public void Test()
    {
        var repo = new Mock<IRepo>();
        repo.Object.Count("a").Returns(ci => _counter++);
        var sut = repo.Object;
    }

    private static int Compute(string name) => name.Length;
}
