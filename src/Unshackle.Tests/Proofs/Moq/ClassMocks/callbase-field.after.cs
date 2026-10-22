using NSubstitute;

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
    private readonly Repo _repo = Substitute.ForPartsOf<Repo>(5);

    public void Test()
    {
        var name = _repo.Name();
    }
}
