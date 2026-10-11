using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Holder h)
    {
        h.Items.ShouldNotBeEmpty();
        foreach (var i in h.Items)
        {
            Check(i);
        }
    }

    private static void Check(int i)
    {
    }
}

public class Holder
{
    public List<int> Items { get; set; } = new();
}
