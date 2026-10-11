using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string a, string b)
    {
        (a + b).Should().HaveLength(5);
    }
}
