using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(DateTime t, DateTime u, DateTimeOffset o, DateTimeOffset p, TimeSpan s)
    {
        t.Should().BeCloseTo(u, TimeSpan.FromSeconds(2));
        o.Should().BeCloseTo(p, TimeSpan.FromSeconds(2), "clock");
        s.Should().BeCloseTo(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        t.Should().NotBeCloseTo(u, TimeSpan.FromSeconds(2));
        o.Should().NotBeCloseTo(p, TimeSpan.FromSeconds(2));
        s.Should().NotBeCloseTo(TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
}
