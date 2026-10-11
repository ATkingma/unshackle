using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(bool ok, string name)
    {
        ok.ShouldBeTrue($"{name} must be valid");
    }
}
