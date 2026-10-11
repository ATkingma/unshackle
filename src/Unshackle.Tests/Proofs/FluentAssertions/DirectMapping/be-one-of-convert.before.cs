using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(long x)
    {
        x.Should().BeOneOf(1, 2);
    }
}
