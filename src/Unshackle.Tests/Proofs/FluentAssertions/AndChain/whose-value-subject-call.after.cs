using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(int unused)
    {
        var subject2 = Get();
        subject2.ShouldContainKey("a");
        subject2["a"].ShouldBe(1);
    }

    private static Dictionary<string, int> Get() => new() { ["a"] = 1 };
}
