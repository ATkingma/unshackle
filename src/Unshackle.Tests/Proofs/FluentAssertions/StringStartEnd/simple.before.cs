using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.Should().StartWith("Ti");
        s.Should().EndWith("me");
        s.Should().NotStartWith("x");
        s.Should().NotEndWith("x");
    }
}
