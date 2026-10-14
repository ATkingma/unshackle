using System.Collections.Immutable;
using FluentAssertions;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): DistributedData compares ImmutableDictionary entries (TAAK-225).
public class ImmutableDictionaryCase
{
    public void Test(ImmutableDictionary<string, long> entries)
    {
        entries.Should().Equal(ImmutableDictionary<string, long>.Empty.Add("a", 1L).Add("b", 2L));
    }
}
