using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class DictionaryCase
{
    public void Test(Dictionary<string, int> x)
    {
        x.Should().HaveCount(2);
    }
}
