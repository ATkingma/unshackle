using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(DateTime? a, DateTime b)
    {
        a.Should().BeAfter(b);
    }
}
