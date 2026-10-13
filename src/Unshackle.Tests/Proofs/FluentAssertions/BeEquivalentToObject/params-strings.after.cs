using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> names, List<string?> maybe)
    {
        names.ShouldBe(new string[] { "A", "B" }, ignoreOrder: true);
        names.ShouldBe(new string[] { "A" }, ignoreOrder: true);
        maybe.ShouldBe(new string[] { "A", null }, ignoreOrder: true);
        names.ShouldBe(new string[] { }, ignoreOrder: true);
    }
}
