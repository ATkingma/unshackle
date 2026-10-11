using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class BecauseMessageCase
{
    public void Test(Order order)
    {
        Should.Throw<InvalidOperationException>(() => order.Cancel(), "it is shipped").Message.ShouldEndWith("shipped", Shouldly.Case.Insensitive);
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
