using FluentAssertions;

namespace Proofs;

public class Dto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Total { get; set; }
}

public class Case
{
    public void Test(Dto a, Dto b)
    {
        a.Should().BeEquivalentTo(b, o => o.Using<decimal>(c => c.Subject.Should().BeApproximately(c.Expectation, 0.01m)).WhenTypeIs<decimal>());
    }
}
