using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class BecauseArgs
{
    public void Test(List<int> x, int y)
    {
        x.Count.ShouldBe(2, string.Format(System.Globalization.CultureInfo.InvariantCulture, "x {0}", y));
    }
}
