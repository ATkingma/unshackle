using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class NotContainCase
{
    public void Test(List<int> x, string text)
    {
        x.ShouldNotContain(2);
        text.ShouldNotContain("bad", Shouldly.Case.Sensitive);
    }
}
