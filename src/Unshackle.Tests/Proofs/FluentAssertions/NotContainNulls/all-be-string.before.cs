using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> x)
    {
        x.Should().AllBe("EUR");
    }
}
