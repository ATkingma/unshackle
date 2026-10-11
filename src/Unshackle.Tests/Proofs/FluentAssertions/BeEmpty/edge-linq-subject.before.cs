using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<int> items)
    {
        items.Where(x => x < 0).Should().BeEmpty();
    }
}
