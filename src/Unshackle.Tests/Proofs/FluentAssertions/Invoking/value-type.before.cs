using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ValueTypeCase
{
    public void Test(DateTime date)
    {
        date.Invoking(d => d.AddYears(100000)).Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class Order
{
    public void Cancel() => throw new InvalidOperationException("Order shipped");

    public string Name() => throw new InvalidOperationException();

    public Task CancelAsync() => Task.FromException(new InvalidOperationException());
}
