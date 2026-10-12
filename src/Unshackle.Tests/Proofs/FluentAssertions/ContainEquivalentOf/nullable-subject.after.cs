#nullable enable
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<string>? x)
    {
        x.ShouldNotBeNull().ShouldContain("a");
        x.ShouldNotContain("b");
    }
}
