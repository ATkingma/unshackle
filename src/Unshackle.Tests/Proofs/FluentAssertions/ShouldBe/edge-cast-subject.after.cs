using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(object value)
    {
        ((string)value).ShouldBe("a");
    }
}
