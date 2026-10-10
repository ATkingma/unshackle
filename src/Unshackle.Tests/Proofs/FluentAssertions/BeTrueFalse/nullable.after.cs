using Shouldly;

namespace Proofs;

public class Nullable
{
    public void Test(bool? flag)
    {
        flag.ShouldBe(true);
        flag.ShouldBe(false, "why");
    }
}
