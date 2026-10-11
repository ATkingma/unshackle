using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.ShouldStartWith("ti");
        s.ShouldEndWith("ME");
        s.ShouldNotStartWith("x");
        s.ShouldNotEndWith("x");
    }
}
