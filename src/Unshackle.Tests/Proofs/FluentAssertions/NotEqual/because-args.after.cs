using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, List<int> y)
    {
        x.ShouldNotBeNull().ShouldNotBe(y, string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 2));
    }
}
