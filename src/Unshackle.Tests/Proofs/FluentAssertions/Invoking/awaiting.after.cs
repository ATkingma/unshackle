using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class AwaitingCase
{
    public async Task Test(Order order)
    {
        await Should.ThrowAsync<InvalidOperationException>(() => order.CancelAsync());
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
