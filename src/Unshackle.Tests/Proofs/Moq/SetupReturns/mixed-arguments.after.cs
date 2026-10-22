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
    public void Test()
    {
        var repo = new Mock<IRepo>();
        repo.Object.Find(Arg.Is<int>(1), Arg.Is<string>(n => n.Length > 2)).Returns("found");
        var sut = repo.Object;
    }
}
