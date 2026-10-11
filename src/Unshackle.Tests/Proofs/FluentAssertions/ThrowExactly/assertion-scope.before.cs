using System;
using FluentAssertions.Execution;
using FluentAssertions;

namespace Proofs;

public class AssertionScopeCase
{
    public void Test(Action act)
    {
        using (new AssertionScope())
        {
            act.Should().ThrowExactly<InvalidOperationException>();
        }
    }
}
