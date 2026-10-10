using FluentAssertions;
using System.Collections.Generic;
using System.Linq;

namespace Proofs;

public class EnumerableExistingUsing
{
    public void Test(IEnumerable<int> x)
    {
        x.Should().HaveCount(2);
    }
}
