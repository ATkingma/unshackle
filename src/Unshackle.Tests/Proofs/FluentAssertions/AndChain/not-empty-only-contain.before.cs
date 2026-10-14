using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

// Shape from LiteDB (MIT): OnlyContain already asserts a non-empty collection, so NotBeEmpty before it is not repeated.
public class NotEmptyOnlyContainCase
{
    public void Test(List<long> positions)
    {
        positions.Should().NotBeEmpty().And.OnlyContain(p => p % 8192 == 0);
    }
}
