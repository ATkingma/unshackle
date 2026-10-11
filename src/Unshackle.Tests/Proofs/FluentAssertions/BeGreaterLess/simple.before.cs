using System;
using FluentAssertions;

namespace Proofs;

public class SimpleCase
{
    public void Test(int x, decimal d, double f, long l)
    {
        x.Should().BeGreaterThan(1);
        x.Should().BeGreaterThanOrEqualTo(2);
        d.Should().BeLessThan(3m);
        f.Should().BeLessThanOrEqualTo(4.5);
        l.Should().BeGreaterOrEqualTo(5);
        l.Should().BeLessOrEqualTo(6);
    }
}
