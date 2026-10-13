using System.Collections;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class ExplicitCountCase
{
    public void Test(Items x)
    {
        x.Should().HaveCount(1);
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
