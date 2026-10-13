using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace Proofs;

public class StructCase
{
    public void Test(Dictionary<string, int> entries)
    {
        var entry = entries.FirstOrDefault(e => e.Key.Contains("journal"));
        ((object)entry).ShouldNotBeNull("the check should be registered");
    }
}
