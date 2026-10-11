using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(List<int> a)
    {
        a.Count.ShouldBeGreaterThan(1, string.Format(System.Globalization.CultureInfo.InvariantCulture, "id {0}", 1));
    }
}
