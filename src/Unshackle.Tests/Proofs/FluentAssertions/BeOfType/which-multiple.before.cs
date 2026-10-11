using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        Dto a = obj.Should().BeOfType<Dto>().Which, b = new Dto();
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
