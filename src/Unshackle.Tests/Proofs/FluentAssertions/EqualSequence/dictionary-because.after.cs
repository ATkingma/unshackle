using System.Collections.Generic;
using System.Collections.Immutable;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IImmutableDictionary<string, int?> x, Dictionary<string, int?> y)
    {
        x.ShouldBe(y, ignoreOrder: true, string.Format(System.Globalization.CultureInfo.InvariantCulture, "the {0} counts", "node"));
    }
}
