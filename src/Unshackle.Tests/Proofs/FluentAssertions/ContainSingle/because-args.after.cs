using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldHaveSingleItem(string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 1));
    }
}
