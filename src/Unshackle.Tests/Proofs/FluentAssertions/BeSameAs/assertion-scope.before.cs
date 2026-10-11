using FluentAssertions;
using FluentAssertions.Execution;
using System;

namespace Proofs;

public class Case
{
    public void Test(object a, object b)
    {
        using var scope = new AssertionScope();
        a.Should().BeSameAs(b);
    }
}
