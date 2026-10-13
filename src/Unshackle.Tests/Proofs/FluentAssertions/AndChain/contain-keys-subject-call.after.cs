using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(int unused)
    {
        var subject = Get();
        subject.ShouldContainKey("a");
        subject.ShouldContainKey("b");
    }

    private static Dictionary<string, int> Get() => new() { ["a"] = 1, ["b"] = 2 };
}
