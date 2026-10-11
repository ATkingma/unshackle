using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AwaitingExactlyCase
{
    public async Task Test(Order order)
    {
        await order.Awaiting(o => o.CancelAsync()).Should().ThrowExactlyAsync<InvalidOperationException>();
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
