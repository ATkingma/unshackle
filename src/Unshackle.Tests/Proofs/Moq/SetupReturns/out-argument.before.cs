using Moq;

namespace Proofs;

public interface IRepo
{
    int Count(string name);
}

public class SetupProof
{
    private static object Create(out Mock<IRepo> repo)
    {
        repo = new Mock<IRepo>();
        return repo.Object;
    }

    public void Test()
    {
        object sut = Create(out var repo);
        repo.Setup(r => r.Count("a")).Returns(1);
    }
}
