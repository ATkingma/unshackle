using Shouldly;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(string s)
    {
        s.ShouldBe("ab", string.Format(System.Globalization.CultureInfo.InvariantCulture, "reason {0}", 1), StringCompareShould.IgnoreCase);
    }
}
