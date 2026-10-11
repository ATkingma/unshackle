using FluentAssertions;

namespace Proofs;

public class BecauseArgs
{
    public void Test(int total, int items)
    {
        total.Should().NotBe(0, "{0} items", items);
    }
}
