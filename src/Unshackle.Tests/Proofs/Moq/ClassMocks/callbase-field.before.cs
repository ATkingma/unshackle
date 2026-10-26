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
    private readonly Mock<Repo> _repo = new Mock<Repo>(5) { CallBase = true };

    public void Test()
    {
        var name = _repo.Object.Name();
    }
}
