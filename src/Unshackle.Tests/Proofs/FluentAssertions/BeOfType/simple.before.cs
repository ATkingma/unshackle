using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        obj.Should().BeOfType<Dto>();
        obj.Should().BeAssignableTo<object>();
        obj.Should().NotBeOfType<string>();
        obj.Should().NotBeAssignableTo<IDisposable>();
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
