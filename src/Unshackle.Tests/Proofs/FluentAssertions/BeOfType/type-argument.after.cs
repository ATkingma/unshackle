using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        obj.ShouldBeOfType(typeof(Dto));
        obj.ShouldNotBeAssignableTo(typeof(string), "no");
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
