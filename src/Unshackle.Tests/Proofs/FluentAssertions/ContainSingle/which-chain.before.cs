using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<Dto> x)
    {
        decimal price = x.Should().ContainSingle().Which.Price;
    }
}

public class Dto
{
    public decimal Price { get; set; }
}
