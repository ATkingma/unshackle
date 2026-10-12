using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(double d)
    {
        (d < 0).ShouldBeTrue();
    }
}
