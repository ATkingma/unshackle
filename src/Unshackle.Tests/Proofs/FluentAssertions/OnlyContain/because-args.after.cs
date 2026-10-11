using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldNotBeEmpty(string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", "reason"));
        x.ShouldAllBe(i => i > 0, string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", "reason"));
    }
}
