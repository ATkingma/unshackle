using Moq;
using NSubstitute;
using System.Threading.Tasks;

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
    public void Test()
    {
        var repo = new Mock<IRepo>();
        repo.Object.TotalAsync(2020).Returns(Task.FromResult<int>(5));
        var sut = repo.Object;
    }
}
