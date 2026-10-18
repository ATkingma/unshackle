using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class DictionaryCase
{
    public void Test(Dictionary<string, int> x, IReadOnlyDictionary<string, int> y)
    {
        x.Should().HaveCount(2);
        y.Should().HaveCountGreaterThan(1);
    }
}
