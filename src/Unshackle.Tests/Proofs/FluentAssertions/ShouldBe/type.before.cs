using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(Type t, Type? n)
    {
        t.Should().Be<List<int>>();
        t.Should().Be(typeof(List<int>), "the {0} type", "list");
        n.Should().Be<int>("it is set");
    }
}
