using Shouldly;

namespace Proofs;

public class BecauseArgs
{
    public void Test(bool flag, int y)
    {
        flag.ShouldBeTrue(string.Format(System.Globalization.CultureInfo.InvariantCulture, "x {0}", y));
    }
}
