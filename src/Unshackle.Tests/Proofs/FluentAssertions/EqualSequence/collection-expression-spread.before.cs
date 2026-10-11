using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> s, string[] t)
    {
        s.Should().Equal([.. t, "b"]);
    }
}
