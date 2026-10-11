using FluentAssertions;

namespace Proofs;

public class Dto
{
    public int Id { get; set; }
}

public class Case
{
    public void Test(System.Collections.Generic.List<Dto> a, System.Collections.Generic.List<Dto> b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
