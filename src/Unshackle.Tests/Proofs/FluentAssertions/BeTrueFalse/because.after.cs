using Shouldly;

namespace Proofs;

public class Because
{
    public void Test(bool flag)
    {
        flag.ShouldBeTrue("must be paid");
    }
}
