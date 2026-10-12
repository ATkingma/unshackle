using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> x, string currency)
    {
        x.Should().AllBe(currency);
    }
}
