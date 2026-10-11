using System;
using FluentAssertions;

namespace Proofs;

public class NullableCase
{
    public void Test(int? x)
    {
        x.Should().BeGreaterThan(1);
    }
}
