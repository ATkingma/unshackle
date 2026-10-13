using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class FluentActionsCase
{
    public void Test(Order order)
    {
        FluentActions.Invoking(() => order.Cancel()).Should().Throw<InvalidOperationException>();
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
