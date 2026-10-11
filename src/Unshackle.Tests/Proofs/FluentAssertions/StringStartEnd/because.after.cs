using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.ShouldStartWith("Ti", Shouldly.Case.Sensitive, "name");
        s.ShouldNotEndWith("x", "name", Shouldly.Case.Sensitive);
        s.ShouldEndWith("me", Shouldly.Case.Insensitive, "name");
        s.ShouldNotEndWith("x", "name");
        s.ShouldNotBeNullOrEmpty("set");
    }
}
