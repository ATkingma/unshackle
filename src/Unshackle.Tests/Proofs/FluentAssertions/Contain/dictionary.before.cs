using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class DictionaryCase
{
    public void Test(Dictionary<string, int> x)
    {
        x.Should().Contain(new KeyValuePair<string, int>("a", 1));
    }
}
