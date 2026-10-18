using FluentAssertions;
using Shouldly;
using System;

namespace Proofs;

public class Case
{
    public void Test(int a, string? b)
    {
        // the analyzer wants the AndConstraint used
        a.Should().Be(3);
        b.Should().NotBeNull().And.BeEmpty();
    }
}
