using Shouldly;

namespace Proofs;

public class Simple
{
    public void Test(int result, string name)
    {
        result.ShouldNotBe(41);
        name.ShouldNotBe("timme");
    }
}
