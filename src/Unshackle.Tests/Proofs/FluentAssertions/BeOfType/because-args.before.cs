using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        obj.Should().BeOfType<Dto>("of {0}", 1);
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
