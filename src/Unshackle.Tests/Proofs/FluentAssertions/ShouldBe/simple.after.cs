using Shouldly;

namespace Proofs;

public class Simple
{
    public void Test(int result, string name)
    {
        result.ShouldBe(42);
        name.ShouldBe("timme");
    }
}
