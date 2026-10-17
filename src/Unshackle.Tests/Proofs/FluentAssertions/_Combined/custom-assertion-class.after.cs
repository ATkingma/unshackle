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

    public int Total { get; set; }
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
}

public class CustomAssertionClass
{
    public void Test(Order order)
    {
        order.ShouldBeCancelled("we just cancelled it");
        order.ShouldNotBeNull();
        order.Total.ShouldBe(0);
    }
}
