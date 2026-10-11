using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        obj.ShouldBeOfType<Dto>();
        obj.ShouldBeAssignableTo<object>();
        obj.ShouldNotBeOfType<string>();
        obj.ShouldNotBeAssignableTo<IDisposable>();
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
