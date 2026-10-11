using FluentAssertions;

namespace Proofs;

public class Dto
{
    public int Id { get; set; }

    public override bool Equals(object obj) => obj is Dto d && d.Id == Id;

    public override int GetHashCode() => Id;
}

public class Case
{
    public void Test(Dto a, Dto b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
