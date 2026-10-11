using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.Should().StartWith("Ti", "name");
        s.Should().NotEndWith("x", "name");
        s.Should().EndWithEquivalentOf("me", "name");
        s.Should().NotEndWithEquivalentOf("x", "name");
        s.Should().NotBeNullOrEmpty("set");
    }
}
