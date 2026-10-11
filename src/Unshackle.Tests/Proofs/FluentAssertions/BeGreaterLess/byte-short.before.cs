using System;
using FluentAssertions;

namespace Proofs;

public class ByteShortCase
{
    public void Test(byte b, short s)
    {
        b.Should().BeGreaterThan(1);
        s.Should().BeLessOrEqualTo(10, "small");
        b.Should().BeInRange(1, 5);
    }
}
