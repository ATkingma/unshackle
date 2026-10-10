using Shouldly;

namespace Proofs;

public class BeTrue
{
    public void Test(bool flag)
    {
        flag.ShouldBeTrue();
    }
}
