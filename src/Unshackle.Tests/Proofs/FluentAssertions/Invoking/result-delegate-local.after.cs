using System;
using System.Threading.Tasks;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultDelegateLocalCase
{
    public async Task Test(Order order, Func<Order, Task> action)
    {
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => action(order));
        ex.Message.Should().Be("Order shipped");
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
