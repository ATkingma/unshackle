using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(int? x)
    {
        x.ShouldBeNull("not set");
    }
}
