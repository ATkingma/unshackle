using System;
using Shouldly;

namespace Proofs;

public class ComparableCase
{
    public void Test(TimeSpan t, Version v)
    {
        t.ShouldBeGreaterThan(TimeSpan.Zero);
        v.ShouldBeLessThan(new Version(2, 0));
    }
}
