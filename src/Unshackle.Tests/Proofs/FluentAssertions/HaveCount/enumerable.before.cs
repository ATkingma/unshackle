using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class EnumerableCase
{
    public void Test(IEnumerable<int> x)
    {
        x.Should().HaveCount(2);
        Numbers().Should().HaveCount(1);
    }

    private static IEnumerable<int> Numbers()
    {
        yield return 1;
    }
}
