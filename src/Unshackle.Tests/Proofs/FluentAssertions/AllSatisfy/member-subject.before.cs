using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Holder h)
    {
        h.Items.Should().AllSatisfy(i => Check(i));
    }

    private static void Check(int i)
    {
    }
}

public class Holder
{
    public List<int> Items { get; set; } = new();
}
