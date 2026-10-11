using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        var dto = obj.ShouldBeOfType<Dto>();
        var asObject = obj.ShouldBeAssignableTo<object>("any");
        dto.Name = "x";
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
