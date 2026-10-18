using System;
using FluentAssertions;
using FluentAssertions.Common;

namespace Proofs;

public class Case
{
    public void Test(int x)
    {
        x.Should().Be(1);
    }
}
