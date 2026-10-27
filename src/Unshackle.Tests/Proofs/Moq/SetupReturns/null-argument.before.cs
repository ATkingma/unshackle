using Moq;

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
        repo.Setup(r => r.Find(It.IsAny<int>(), null)).Returns("found");
        var sut = repo.Object;
    }
}
