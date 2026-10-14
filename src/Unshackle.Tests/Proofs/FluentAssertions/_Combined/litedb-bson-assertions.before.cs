using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

// Shape from LiteDB (MIT): query tests with BeOneOf, Equal on arrays and OnlyHaveUniqueItems.
public class BsonAssertionsCase
{
    public void Test(int[] docs, string first, List<long> positions, byte[] actual, byte[] expected)
    {
        docs.Length.Should().BeOneOf(1, 101);
        first.Should().BeOneOf("owner1", "owner2");
        actual.Should().Equal(expected);
        positions.Should().OnlyHaveUniqueItems();
        positions.Should().NotBeEmpty().And.OnlyContain(p => p % 8192 == 0);
    }
}
