using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class PredicateCase
{
    public void Test(List<string> x)
    {
        x.Should().Contain(e => e.StartsWith("a"));
        x.Should().NotContain(e => e.Length > 3);
    }
}
