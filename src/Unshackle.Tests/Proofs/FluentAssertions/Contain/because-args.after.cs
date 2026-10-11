using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(List<int> x)
    {
        x.ShouldContain(2, string.Format(System.Globalization.CultureInfo.InvariantCulture, "it was {0}", "added"));
    }
}
