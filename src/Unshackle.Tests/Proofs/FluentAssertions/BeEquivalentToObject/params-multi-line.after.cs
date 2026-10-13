using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<string> lines)
    {
        lines.ShouldBe(new string[]
        {
            "first",
            // a blank line
            "",
            "last",
        }, ignoreOrder: true);
    }
}
