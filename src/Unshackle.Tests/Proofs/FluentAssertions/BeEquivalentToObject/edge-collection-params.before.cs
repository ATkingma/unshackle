using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<int> numbers)
    {
        numbers.Select(n => n.ToString()).Should().BeEquivalentTo("1", "2");
    }
}
