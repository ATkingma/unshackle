using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(List<int> a)
    {
        a.Should().HaveCountGreaterThan(1, "id {0}", 1);
    }
}
