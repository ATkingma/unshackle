using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<Dto> x)
    {
        decimal price = x.ShouldHaveSingleItem().Price;
    }
}

public class Dto
{
    public decimal Price { get; set; }
}
