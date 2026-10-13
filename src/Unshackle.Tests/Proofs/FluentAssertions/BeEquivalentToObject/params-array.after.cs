using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> names, string[] expected)
    {
        names.ShouldBe(expected, ignoreOrder: true);
        names.ShouldBe(new[] { "A", "B" }, ignoreOrder: true);
    }
}
