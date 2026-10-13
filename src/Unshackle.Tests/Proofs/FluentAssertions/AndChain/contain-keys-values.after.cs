using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, string> contextData)
    {
        contextData.ShouldContainKey("key1");
        contextData.ShouldContainKey("key2");
        contextData.Values.ShouldContain("value1");
        contextData.Values.ShouldContain("value2");
    }
}
