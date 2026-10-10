using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Proofs;

public class EnumerableCase
{
    public void Test(IEnumerable<int> x)
    {
        x.Count().ShouldBe(2);
        Numbers().Count().ShouldBe(1);
    }

    private static IEnumerable<int> Numbers()
    {
        yield return 1;
    }
}
