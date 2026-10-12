using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Dto
{
    public int Id { get; set; }
}

public class Case
{
    public void Test(List<Dto> x)
    {
        x.Should().ContainEquivalentOf(new Dto { Id = 1 });
    }
}
