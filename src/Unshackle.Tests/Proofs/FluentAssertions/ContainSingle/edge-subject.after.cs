using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<string> names)
    {
        var name = names.ShouldHaveSingleItem();
        Console.WriteLine(name);
    }
}
