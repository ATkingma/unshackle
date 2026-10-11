#nullable enable
using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class NullableHaveCount
{
    public void Test(List<int>? x)
    {
        x.ShouldNotBeNull().Count.ShouldBe(2);
    }
}
