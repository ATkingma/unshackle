using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        string name = obj.ShouldBeOfType<Dto>().Name;
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
