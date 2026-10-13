using System;
using System.Threading.Tasks;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultAndCase
{
    public void Test(Order order)
    {
        Should.Throw<InvalidOperationException>(() => order.Cancel()).Message.Should().Be("Order shipped");
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
