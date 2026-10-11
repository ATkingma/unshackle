using System;
using System.Collections.Generic;
using Shouldly;
using System.Linq;

namespace Proofs;

public class Case
{
    public void Test(List<Dto> x)
    {
        var only = x.ShouldHaveSingleItem();
        var big = x.Where(d => d.Price > 5m).ShouldHaveSingleItem("big");
        only.Price = big.Price;
    }
}

public class Dto
{
    public decimal Price { get; set; }
}
