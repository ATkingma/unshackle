using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MethodGroupCase
{
    public void Test(Order order)
    {
        Should.Throw<InvalidOperationException>(() => Run(order));
    }

    private static void Run(Order order) => order.Cancel();
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
