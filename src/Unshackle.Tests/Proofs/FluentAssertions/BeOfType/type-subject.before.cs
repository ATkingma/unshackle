using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        typeof(Dto).Should().BeAssignableTo<object>();
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
