using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> x)
    {
        x.Should().OnlyContain(s => s.Length > 1);
    }
}
