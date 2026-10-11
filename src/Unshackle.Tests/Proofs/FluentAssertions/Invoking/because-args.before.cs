using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(Order order)
    {
        order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>("a {0}", 1);
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
