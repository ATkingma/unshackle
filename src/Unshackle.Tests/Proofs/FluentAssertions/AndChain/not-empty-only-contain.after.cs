using System.Collections.Generic;
using Shouldly;

namespace Proofs;

// Shape from LiteDB (MIT): OnlyContain already asserts a non-empty collection, so NotBeEmpty before it is not repeated.
public class NotEmptyOnlyContainCase
{
    public void Test(List<long> positions)
    {
        positions.ShouldNotBeEmpty();
        positions.ShouldAllBe(p => p % 8192 == 0);
    }
}
