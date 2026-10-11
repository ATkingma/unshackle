#nullable enable
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string? name)
    {
        name.Should().NotBeNull().And.Be("Ann");
    }
}
