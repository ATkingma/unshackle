using FluentAssertions;

namespace Proofs;

public class Line
{
    public int Qty { get; set; }
}

public class Order
{
    public System.DateTime At { get; set; }
    public System.Guid Id { get; set; }
    public int? Rank { get; set; }
    public Line Main { get; set; }
}

public class Case
{
    public void Test(Order a, Order b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
