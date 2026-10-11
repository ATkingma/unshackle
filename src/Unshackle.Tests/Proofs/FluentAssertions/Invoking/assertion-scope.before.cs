using System;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using FluentAssertions;

namespace Proofs;

public class AssertionScopeCase
{
    public void Test(Order order)
    {
        using (new AssertionScope())
        {
            order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>();
        }
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
