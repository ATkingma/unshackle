using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(double ratio)
    {
        ratio.ShouldBeGreaterThan(0.5);
    }
}
