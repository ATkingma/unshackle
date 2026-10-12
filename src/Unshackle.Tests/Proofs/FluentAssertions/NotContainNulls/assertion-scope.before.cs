using System;
using System.Collections.Generic;
using FluentAssertions.Execution;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> x)
    {
        using var scope = new AssertionScope();
        x.Should().NotContainNulls();
    }
}
