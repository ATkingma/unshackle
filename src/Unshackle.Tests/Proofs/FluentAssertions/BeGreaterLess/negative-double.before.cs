using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(double d)
    {
        d.Should().BeNegative();
    }
}
