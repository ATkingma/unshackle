using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(DateTime t, DateTime u, DateTimeOffset o, DateTimeOffset p, TimeSpan s)
    {
        t.ShouldBe(u, TimeSpan.FromSeconds(2));
        o.ShouldBe(p, TimeSpan.FromSeconds(2), "clock");
        s.ShouldBe(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        t.ShouldNotBe(u, TimeSpan.FromSeconds(2));
        o.ShouldNotBe(p, TimeSpan.FromSeconds(2));
        s.ShouldNotBe(TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
}
