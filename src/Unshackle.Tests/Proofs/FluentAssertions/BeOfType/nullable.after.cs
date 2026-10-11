#nullable enable
using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(object? obj)
    {
        obj.ShouldBeOfType<Dto>();
        Dto dto = obj.ShouldBeOfType<Dto>();
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
