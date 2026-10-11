using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<Dto> x)
    {
        x.Should().ContainSingle().Which.Price.Should().Be(5m);
    }
}

public class Dto
{
    public decimal Price { get; set; }
}
