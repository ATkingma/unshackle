using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class CollectionInterfaces
{
    public void Test(ICollection<int> a, IReadOnlyCollection<int> b, IReadOnlyList<int> c, IList<int> d, HashSet<int> e)
    {
        a.Should().HaveCount(1);
        b.Should().HaveCount(2);
        c.Should().HaveCount(3);
        d.Should().HaveCount(4);
        e.Should().HaveCount(5);
    }
}
