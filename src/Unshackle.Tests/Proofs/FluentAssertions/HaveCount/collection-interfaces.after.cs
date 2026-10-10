using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class CollectionInterfaces
{
    public void Test(ICollection<int> a, IReadOnlyCollection<int> b, IReadOnlyList<int> c, IList<int> d, HashSet<int> e)
    {
        a.Count.ShouldBe(1);
        b.Count.ShouldBe(2);
        c.Count.ShouldBe(3);
        d.Count.ShouldBe(4);
        e.Count.ShouldBe(5);
    }
}
