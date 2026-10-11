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
    public void Test()
    {
        new Dto { Id = 1 }.Should().BeEquivalentTo(new Dto { Id = 1 });
    }
}
