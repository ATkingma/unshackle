using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        using var scope = new AssertionScope();
        s.Should().StartWith("Ti");
    }
}
