using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.Should().StartWith("Ti", "of {0}", 1);
    }
}
