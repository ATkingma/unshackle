using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.ShouldStartWith("Ti", Shouldly.Case.Sensitive, string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 1));
    }
}
