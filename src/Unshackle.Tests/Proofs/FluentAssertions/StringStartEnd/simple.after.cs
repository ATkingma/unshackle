using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.ShouldStartWith("Ti", Shouldly.Case.Sensitive);
        s.ShouldEndWith("me", Shouldly.Case.Sensitive);
        s.ShouldNotStartWith("x", Shouldly.Case.Sensitive);
        s.ShouldNotEndWith("x", Shouldly.Case.Sensitive);
    }
}
