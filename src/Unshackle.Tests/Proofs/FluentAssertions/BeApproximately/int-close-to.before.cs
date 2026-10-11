using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(int i)
    {
        i.Should().BeCloseTo(5, 1);
    }
}
