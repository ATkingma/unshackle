using FluentAssertions;
using System;

namespace Proofs;

public class Case
{
    public void Test(string a, string b)
    {
        a.Should().BeSameAs(b, "cached");
    }
}
