using FluentAssertions;

namespace Proofs;

public class SimpleCase
{
    public void Test(string s)
    {
        s.Should().BeEquivalentTo("ab");
    }
}
