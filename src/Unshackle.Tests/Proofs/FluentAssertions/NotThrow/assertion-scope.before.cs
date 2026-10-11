using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class AssertionScopeCase
{
    public void Test(Action act)
    {
        using var scope = new AssertionScope();
        act.Should().NotThrow();
    }
}
