using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class PredicateCase
{
    public void Test(List<int> x)
    {
        x.Should().HaveCount(c => c > 1);
    }
}
