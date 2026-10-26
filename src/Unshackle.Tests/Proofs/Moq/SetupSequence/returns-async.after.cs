using Moq;
using NSubstitute;
using System.Threading.Tasks;

namespace Proofs;

public interface IRepo
{
    string Label { get; }
    int Count(string name);
    void Send(int id);
    System.Threading.Tasks.Task<int> TotalAsync(int year);
}

public class SequenceProof
{
    public void Test()
    {
        var repo = new Mock<IRepo>();
        repo.Object.TotalAsync(2020).Returns(Task.FromResult<int>(1), Task.FromResult<int>(2));
        var sut = repo.Object;
    }
}
