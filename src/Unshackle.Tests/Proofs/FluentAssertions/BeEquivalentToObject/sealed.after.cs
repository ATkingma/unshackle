using Shouldly;

namespace Proofs;

public sealed class Dto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Total { get; set; }
}

public class Case
{
    public void Test(Dto a, Dto b)
    {
        a.ShouldBeEquivalentTo(b);
    }
}
