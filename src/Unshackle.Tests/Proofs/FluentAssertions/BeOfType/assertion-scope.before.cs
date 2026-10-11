using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        using var scope = new AssertionScope();
        obj.Should().BeOfType<Dto>();
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
