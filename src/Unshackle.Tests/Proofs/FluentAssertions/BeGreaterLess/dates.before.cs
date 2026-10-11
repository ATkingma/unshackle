using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(DateTime a, DateTime b, DateTimeOffset c, DateTimeOffset d)
    {
        a.Should().BeAfter(b);
        a.Should().BeOnOrAfter(b);
        a.Should().BeBefore(b);
        c.Should().BeOnOrBefore(d, "deadline");
    }
}
