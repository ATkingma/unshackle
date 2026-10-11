using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(object value)
    {
        ((string)value).Should().Be("a");
    }
}
