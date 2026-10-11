using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<Dto> x)
    {
        var only = x.Should().ContainSingle().Which;
        var big = x.Should().ContainSingle(d => d.Price > 5m, "big").Which;
        only.Price = big.Price;
    }
}

public class Dto
{
    public decimal Price { get; set; }
}
