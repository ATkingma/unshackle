using FluentAssertions;

namespace Proofs;


public class Case
{
    public void Test(object a, object b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
