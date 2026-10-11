using System;
using Shouldly;

namespace Proofs;

public class ScopeCase
{
    public void Test(int x, string name)
    {
        int y = x + 1;
        y.ShouldSatisfyAllConditions(
            () => { y.ShouldBe(2); },
            () => { name.ShouldStartWith("a", Shouldly.Case.Sensitive); });
    }
}
