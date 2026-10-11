using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dto dto)
    {
        dto.Name.ShouldStartWith($"{dto.Name[0]}", Shouldly.Case.Sensitive);
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
