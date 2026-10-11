using FluentAssertions;

namespace Proofs;

public class NotEquivalentCase
{
    public void Test(string s)
    {
        s.Should().NotBeEquivalentTo("ab");
    }
}
