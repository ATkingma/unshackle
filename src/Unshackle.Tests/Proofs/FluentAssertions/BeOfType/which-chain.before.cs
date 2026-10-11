using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        string name = obj.Should().BeOfType<Dto>().Which.Name;
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
