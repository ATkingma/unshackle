using Moq;
using NSubstitute;

namespace Proofs;

public interface IRepo
{
    string Find(int id, string name);
}

public class SetupProof
{
    public void Test()
    {
        var repo = new Mock<IRepo>();
        repo.Object.Find(Arg.Any<int>(), Arg.Is<string>(a => a == null)).Returns("found");
        var sut = repo.Object;
    }
}
