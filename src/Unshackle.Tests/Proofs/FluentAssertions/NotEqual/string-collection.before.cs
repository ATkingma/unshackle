using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string[] x, string[] y)
    {
        x.Should().NotEqual(y);
    }
}
