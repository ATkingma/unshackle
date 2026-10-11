using System;
using Shouldly;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(int x)
    {
        x.ShouldBeGreaterThan(1, string.Format(System.Globalization.CultureInfo.InvariantCulture, "it is {0}", 1));
    }
}
