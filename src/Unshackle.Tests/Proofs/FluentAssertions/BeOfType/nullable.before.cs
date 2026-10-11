#nullable enable
using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object? obj)
    {
        obj.Should().BeOfType<Dto>();
        Dto dto = obj.Should().BeOfType<Dto>().Which;
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
