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
    public void Test(Dto a)
    {
        a.Should().BeEquivalentTo(new { Id = 1 });
    }
}
