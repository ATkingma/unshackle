using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        var dto = obj.Should().BeOfType<Dto>().Which;
        var asObject = obj.Should().BeAssignableTo<object>("any").Subject;
        dto.Name = "x";
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
