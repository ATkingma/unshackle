using Moq;

namespace Proofs;

public interface IRepo
{
    int Count(string name);
}

public class SetupProof
{
    private static (Mock<IRepo> Repo, object Sut) Create()
    {
        var repo = new Mock<IRepo>();
        return (repo, repo.Object);
    }

    public void Test()
    {
        var (repo, sut) = Create();
        repo.Setup(r => r.Count("a")).Returns(1);
    }
}
