using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> s, List<int> x)
    {
        s.Should().Equal(["a", "b"]);
        x.Should().Equal([1]);
    }
}
