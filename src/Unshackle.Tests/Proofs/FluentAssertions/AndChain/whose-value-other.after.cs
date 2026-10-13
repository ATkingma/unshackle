using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, string> x)
    {
        x.ShouldContainKey("a");
        x["a"].ShouldStartWith("b", Shouldly.Case.Sensitive);
    }
}
