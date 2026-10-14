using System.Collections.Generic;
using Shouldly;

namespace Proofs;

// Shape from LiteDB (MIT): query tests with BeOneOf, Equal on arrays and OnlyHaveUniqueItems.
public class BsonAssertionsCase
{
    public void Test(int[] docs, string first, List<long> positions, byte[] actual, byte[] expected)
    {
        docs.Length.ShouldBeOneOf(1, 101);
        first.ShouldBeOneOf("owner1", "owner2");
        actual.ShouldBe(expected);
        positions.ShouldBeUnique();
        positions.ShouldNotBeEmpty();
        positions.ShouldAllBe(p => p % 8192 == 0);
    }
}
