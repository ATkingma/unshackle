using FluentAssertions;

namespace Proofs;

public class ChainCase
{
    public void Test(string s)
    {
        s.Should().BeEquivalentTo("ab").And.HaveLength(2);
    }
}
