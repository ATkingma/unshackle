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
        var shape = new Mock<Shape>() { CallBase = true };
        var drawn = shape.Object.Draw();
    }
}
