using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Proofs;

public class EnumerableBecause
{
    public void Test(IEnumerable<int> x)
    {
        x.Count().ShouldBe(2, "two items expected");
    }
}
