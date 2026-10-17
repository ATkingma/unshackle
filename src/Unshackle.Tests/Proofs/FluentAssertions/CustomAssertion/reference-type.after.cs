using Shouldly;

namespace Proofs;

public enum OrderStatus
{
    Open,
    Cancelled,
}

public class Order
{
    public OrderStatus Status { get; set; }

    public int Lines { get; set; }
}

public static class OrderAssertions
{
    public static void ShouldBeCancelled(this Order subject, string customMessage = null)
    {
        bool condition = subject.Status == OrderStatus.Cancelled;
        var actual = subject.Status;
        if (!condition)
        {
            throw new ShouldAssertException($"Expected order to be cancelled{(customMessage is null ? "" : " because " + customMessage)}, but it is {actual}.");
        }
    }

    public static void ShouldHaveLines(this Order subject, int expected)
    {
        bool condition = subject.Lines == expected;
        var actual1 = subject.Lines;
        if (!condition)
        {
            throw new ShouldAssertException($"Expected order to have {expected} lines, but found {actual1}.");
        }
    }
}

public class ReferenceType
{
    public void Test(Order order)
    {
        order.ShouldBeCancelled("we just cancelled it");
        order.ShouldHaveLines(2);
        order.ShouldBeCancelled();
        order.ShouldHaveLines(1);
    }
}
