using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class FuncCase
{
    public void Test(Order order)
    {
        order.Invoking(o => o.Name()).Should().Throw<InvalidOperationException>();
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
