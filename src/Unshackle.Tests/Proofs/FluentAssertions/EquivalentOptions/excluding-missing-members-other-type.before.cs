using FluentAssertions;

namespace Proofs;

public class Dto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Total { get; set; }
}

public class Other
{
    public int Id { get; set; }
}

public class Case
{
    public void Test(Dto a, Other b)
    {
        a.Should().BeEquivalentTo(b, o => o.ExcludingMissingMembers());
    }
}
