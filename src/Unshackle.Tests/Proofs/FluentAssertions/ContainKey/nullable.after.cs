#nullable enable
using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class NullableContainKey
{
    public void Test(Dictionary<string, int>? x)
    {
        x.ShouldNotBeNull().ShouldContainKey("a");
    }
}
