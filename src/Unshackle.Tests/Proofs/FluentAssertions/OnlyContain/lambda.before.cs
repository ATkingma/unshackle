using System.Collections.Generic;
using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        Action act = () => x.Should().OnlyContain(i => i > 0);
    }
}
