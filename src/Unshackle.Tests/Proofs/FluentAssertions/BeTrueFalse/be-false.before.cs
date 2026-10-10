using FluentAssertions;

namespace Proofs;

public class BeFalse
{
    public void Test(bool flag, int a, int b)
    {
        (a == b).Should().BeFalse();
    }
}
