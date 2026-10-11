using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<int> items)
    {
        items.Any(x => x > 1).ShouldBeFalse();
    }
}
