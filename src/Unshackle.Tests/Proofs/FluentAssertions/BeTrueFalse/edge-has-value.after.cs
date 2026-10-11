using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int? value)
    {
        value.HasValue.ShouldBeTrue();
    }
}
