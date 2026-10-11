using FluentAssertions;

namespace Proofs;

public class BecauseCase
{
    public void Test(string s)
    {
        s.Should().BeEquivalentTo("ab", "names are case-free");
    }
}
