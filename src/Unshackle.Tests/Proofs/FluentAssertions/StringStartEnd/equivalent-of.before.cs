using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.Should().StartWithEquivalentOf("ti");
        s.Should().EndWithEquivalentOf("ME");
        s.Should().NotStartWithEquivalentOf("x");
        s.Should().NotEndWithEquivalentOf("x");
    }
}
