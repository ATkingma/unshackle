using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Proofs;

public class StructCase
{
    public void Test(Dictionary<string, int> entries)
    {
        var entry = entries.FirstOrDefault(e => e.Key.Contains("journal"));
        entry.Should().NotBeNull("the check should be registered");
    }
}
