using System;
using FluentAssertions;

namespace Proofs;

public class ComparableCase
{
    public void Test(TimeSpan t, Version v)
    {
        t.Should().BeGreaterThan(TimeSpan.Zero);
        v.Should().BeLessThan(new Version(2, 0));
    }
}
