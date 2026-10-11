using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.ShouldMatch("^SO", "ids start with SO");
    }
}
