#nullable enable
using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<string>? x)
    {
        x.ShouldNotBeNull().ShouldNotContain(i => i == null);
    }
}
