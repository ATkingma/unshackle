using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class MethodGroupCase
{
    public void Test(Order order)
    {
        order.Invoking(Run).Should().Throw<InvalidOperationException>();
    }

    private static void Run(Order order) => order.Cancel();
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
