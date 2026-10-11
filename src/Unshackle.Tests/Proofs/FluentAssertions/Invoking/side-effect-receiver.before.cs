using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class SideEffectReceiverCase
{
    public void Test()
    {
        Create().Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>();
    }

    private static Order Create() => new();
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
