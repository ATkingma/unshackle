using Shouldly;

namespace Proofs;

public class SimpleCase
{
    public void Test(string s)
    {
        s.ShouldBe("ab", StringCompareShould.IgnoreCase);
    }
}
