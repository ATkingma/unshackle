using Moq;

namespace Proofs;

public class Repo
{
    public Repo(int size)
    {
        Size = size;
    }

    public int Size { get; }

    public virtual string Name() => "repo" + Size;

    public string Plain() => "plain";
}

public abstract class Shape
{
    public abstract string Draw();
}

public class ClassProof
{
    public void Test()
    {
        var repo = new Mock<Repo>(5) { CallBase = true };
        repo.Setup(r => r.Name()).Returns("fake");
        var name = repo.Object.Name();
    }
}
