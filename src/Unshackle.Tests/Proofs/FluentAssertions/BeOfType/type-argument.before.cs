using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        obj.Should().BeOfType(typeof(Dto));
        obj.Should().NotBeAssignableTo(typeof(string), "no");
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
