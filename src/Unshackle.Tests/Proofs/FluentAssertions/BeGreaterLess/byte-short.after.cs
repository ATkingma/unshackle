using System;
using Shouldly;

namespace Proofs;

public class ByteShortCase
{
    public void Test(byte b, short s)
    {
        b.ShouldBeGreaterThan((byte)1);
        s.ShouldBeLessThanOrEqualTo((short)10, "small");
        b.ShouldBeInRange((byte)1, (byte)5);
    }
}
