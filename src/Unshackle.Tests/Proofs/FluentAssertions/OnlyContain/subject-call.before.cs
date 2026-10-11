using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    private List<int> Load() => new();

    public void Test(int y)
    {
        Load().Should().OnlyContain(i => i > y);
    }
}
