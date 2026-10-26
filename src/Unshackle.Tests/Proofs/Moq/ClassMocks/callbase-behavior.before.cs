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
        var repo = new Mock<Repo>(MockBehavior.Loose, 5, 6) { CallBase = true };
        var name = repo.Object.Name();
    }
}
