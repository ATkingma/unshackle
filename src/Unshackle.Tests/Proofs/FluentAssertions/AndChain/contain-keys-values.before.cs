using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, string> contextData)
    {
        contextData.Should().ContainKeys("key1", "key2").And.ContainValues("value1", "value2");
    }
}
