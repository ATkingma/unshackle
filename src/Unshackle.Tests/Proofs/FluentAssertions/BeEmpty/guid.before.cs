using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Guid id, Guid? other)
    {
        id.Should().NotBeEmpty();
        id.Should().BeEmpty("it is new");
        other.Should().BeEmpty();
    }
}
