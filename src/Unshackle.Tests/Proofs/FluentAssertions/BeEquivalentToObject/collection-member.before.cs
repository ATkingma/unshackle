using FluentAssertions;

namespace Proofs;

public class Dto
{
    public System.Collections.Generic.List<int> Ids { get; set; }
}

public class Case
{
    public void Test(Dto a, Dto b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
