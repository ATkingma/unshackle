using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Dto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Total { get; set; }
}

public class Case
{
    public void Test(List<Dto> a, Dto[] b)
    {
        a.Should().BeEquivalentTo(b, o => o.Excluding(x => x.Id));
    }
}
