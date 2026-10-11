using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(System.IO.Stream s)
    {
        s.Should().HaveLength(5);
    }
}
