#nullable enable
using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(double? d, DateTime? t, DateTime u)
    {
        d.ShouldNotBeNull().ShouldBe(1.5, 0.01);
        t.ShouldNotBeNull().ShouldBe(u, TimeSpan.FromSeconds(1));
    }
}
