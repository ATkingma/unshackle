using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        Dto a = obj.ShouldBeOfType<Dto>(), b = new Dto();
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
