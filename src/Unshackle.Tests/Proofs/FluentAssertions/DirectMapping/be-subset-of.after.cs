using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, List<int> all)
    {
        x.ShouldBeSubsetOf(all, "because");
    }
}
