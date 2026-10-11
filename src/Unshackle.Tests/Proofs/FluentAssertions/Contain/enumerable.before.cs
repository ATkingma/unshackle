using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EnumerableCase
{
    public void Test(IEnumerable<string> x, string[] arr)
    {
        x.Should().Contain("a");
        arr.Should().Contain("b");
    }
}
