using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> x)
    {
        x.Should().AllBe(x[0] + "!");
    }
}
