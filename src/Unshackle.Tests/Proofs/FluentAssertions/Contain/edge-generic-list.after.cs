using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test<T>(List<T> items, T item)
    {
        items.ShouldContain(item);
    }
}
