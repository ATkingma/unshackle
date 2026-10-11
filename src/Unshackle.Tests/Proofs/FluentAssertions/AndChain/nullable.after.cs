#nullable enable
using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string? name)
    {
        name.ShouldNotBeNull();
        name.ShouldBe("Ann");
    }
}
