using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ResultAndCase
{
    public void Test(Order order)
    {
        order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>()
            .And.Message.Should().Be("Order shipped");
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
