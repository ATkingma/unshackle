using FluentAssertions;
using FluentAssertions.Equivalency;
using System;

namespace Proofs;

public class Dto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Total { get; set; }
}

public class Case
{
    public void Test(Dto a, Dto b, Func<EquivalencyAssertionOptions<Dto>, EquivalencyAssertionOptions<Dto>> configure)
    {
        a.Should().BeEquivalentTo(b, configure);
    }
}
