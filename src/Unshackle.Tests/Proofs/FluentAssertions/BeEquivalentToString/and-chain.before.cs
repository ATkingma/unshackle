using FluentAssertions;

namespace Proofs;

public class AndChainCase
{
    public void Test(string s)
    {
        s.Should().NotBeNull().And.BeEquivalentTo("ab");
    }
}
