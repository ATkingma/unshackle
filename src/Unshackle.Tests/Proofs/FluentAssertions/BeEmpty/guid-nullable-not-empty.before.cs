using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Guid? id)
    {
        id.Should().NotBeEmpty();
    }
}
