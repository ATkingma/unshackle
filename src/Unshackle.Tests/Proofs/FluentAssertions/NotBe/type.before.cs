using FluentAssertions;
using System;

namespace Proofs;

public class Case
{
    public void Test(Type t, Type? n)
    {
        t.Should().NotBe<string>();
        n.Should().NotBe(typeof(int));
    }
}
