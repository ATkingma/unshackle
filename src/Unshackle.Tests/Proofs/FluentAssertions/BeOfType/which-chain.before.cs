using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        obj.Should().BeOfType<Dto>().Which.Name.Should().Be("a");
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
