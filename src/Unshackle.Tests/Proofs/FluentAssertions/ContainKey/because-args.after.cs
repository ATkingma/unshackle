using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.ShouldContainKey("a", string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", 1));
    }
}
