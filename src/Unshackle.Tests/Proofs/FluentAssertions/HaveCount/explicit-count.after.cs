using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace Proofs;

public class ExplicitCountCase
{
    public void Test(Items x)
    {
        x.Count().ShouldBe(1);
    }
}

public class Items : IReadOnlyCollection<int>
{
    int IReadOnlyCollection<int>.Count => 1;

    public IEnumerator<int> GetEnumerator()
    {
        yield return 1;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
