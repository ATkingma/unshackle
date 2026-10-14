using System.Collections.Immutable;
using Shouldly;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): DistributedData compares ImmutableDictionary entries (TAAK-225).
public class ImmutableDictionaryCase
{
    public void Test(ImmutableDictionary<string, long> entries)
    {
        entries.ShouldBe(ImmutableDictionary<string, long>.Empty.Add("a", 1L).Add("b", 2L), ignoreOrder: true);
    }
}
