using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldContain(2, "it was added");
        x.ShouldNotContain(5, string.Format(System.Globalization.CultureInfo.InvariantCulture, "it was {0}", "removed"));
    }
}
