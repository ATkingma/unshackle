using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(Order order)
    {
        Should.Throw<InvalidOperationException>(() => order.Cancel(), string.Format(System.Globalization.CultureInfo.InvariantCulture, "a {0}", 1));
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
