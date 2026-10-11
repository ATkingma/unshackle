using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class PredicateCase
{
    public void Test(List<string> x)
    {
        x.ShouldContain(e => e.StartsWith("a"));
        x.ShouldNotContain(e => e.Length > 3);
    }
}
