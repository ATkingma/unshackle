using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ResultDelegateLocalCase
{
    public async Task Test(Order order, Func<Order, Task> action)
    {
        var ex = await order.Awaiting(action).Should().ThrowAsync<InvalidOperationException>();
        ex.And.Message.Should().Be("Order shipped");
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
