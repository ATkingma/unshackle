using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Proofs;

public class EnumerableExistingUsing
{
    public void Test(IEnumerable<int> x)
    {
        x.Count().ShouldBe(2);
    }
}
