using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<string> names)
    {
        var name = names.Should().ContainSingle().Subject;
        Console.WriteLine(name);
    }
}
