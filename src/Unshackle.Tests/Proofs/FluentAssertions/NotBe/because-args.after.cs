using Shouldly;

namespace Proofs;

public class BecauseArgs
{
    public void Test(int total, int items)
    {
        total.ShouldNotBe(0, string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} items", items));
    }
}
