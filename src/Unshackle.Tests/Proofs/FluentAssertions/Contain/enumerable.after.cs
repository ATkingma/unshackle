using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EnumerableCase
{
    public void Test(IEnumerable<string> x, string[] arr)
    {
        x.ShouldContain("a");
        arr.ShouldContain("b");
    }
}
