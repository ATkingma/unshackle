using FluentAssertions;
using System;

namespace Proofs;

public class Case
{
    public void Test(int a, string? b)
    {
        // the analyzer wants the AndConstraint used
        _ = a.Should().Be(3);
        _ = b.Should().NotBeNull().And.BeEmpty();
    }
}
