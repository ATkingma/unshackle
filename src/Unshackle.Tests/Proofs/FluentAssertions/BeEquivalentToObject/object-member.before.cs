using FluentAssertions;

namespace Proofs;

public class Dto
{
    public object Value { get; set; }
}

public class Case
{
    public void Test(Dto a, Dto b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
