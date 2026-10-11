using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dto dto)
    {
        dto.Name.Should().StartWith($"{dto.Name[0]}");
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
