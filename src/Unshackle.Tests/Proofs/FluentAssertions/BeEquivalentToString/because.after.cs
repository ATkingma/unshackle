using Shouldly;

namespace Proofs;

public class BecauseCase
{
    public void Test(string s)
    {
        s.ShouldBe("ab", "names are case-free", StringCompareShould.IgnoreCase);
    }
}
