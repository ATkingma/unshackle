using FluentAssertions;

namespace Proofs;

public class ValueUsed
{
    public void Test(bool flag)
    {
        var c = flag.Should().BeFalse();
    }
}
