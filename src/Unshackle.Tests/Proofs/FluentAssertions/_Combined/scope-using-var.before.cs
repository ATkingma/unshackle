using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class ScopeCase
{
    public void Test(int x, string name)
    {
        int y = x + 1;
        using var scope = new AssertionScope();
        y.Should().Be(2);
        name.Should().StartWith("a");
    }
}
