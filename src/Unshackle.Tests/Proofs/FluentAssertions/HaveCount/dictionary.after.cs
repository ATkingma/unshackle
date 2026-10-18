using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class DictionaryCase
{
    public void Test(Dictionary<string, int> x, IReadOnlyDictionary<string, int> y)
    {
        x.Count.ShouldBe(2);
        y.Count.ShouldBeGreaterThan(1);
    }
}
