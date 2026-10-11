using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Enumerable
{
    public void Test(IEnumerable<int> items, int[] array)
    {
        items.ShouldBeEmpty();
        array.ShouldNotBeEmpty();
    }
}
