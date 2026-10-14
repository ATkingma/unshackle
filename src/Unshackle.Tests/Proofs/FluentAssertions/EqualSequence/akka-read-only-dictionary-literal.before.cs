using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): a read-only dictionary against a dictionary initializer (TAAK-225).
public class ReadOnlyDictionaryCase
{
    public void Test(IReadOnlyDictionary<string, int> counts)
    {
        counts.Should().Equal(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
    }
}
