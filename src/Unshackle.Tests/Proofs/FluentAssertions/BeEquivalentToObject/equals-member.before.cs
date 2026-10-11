using FluentAssertions;

namespace Proofs;

public class Money
{
    public override bool Equals(object obj) => true;

    public override int GetHashCode() => 0;
}

public class Dto
{
    public Money Price { get; set; }
}

public class Case
{
    public void Test(Dto a, Dto b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
