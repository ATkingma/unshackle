using FluentAssertions;
using FluentAssertions.Primitives;

namespace Proofs;

public class CancelOrder;

public class CancelOrderAssertions : ObjectAssertions
{
    public CancelOrderAssertions(CancelOrder subject)
        : base(subject)
    {
    }

    public AndConstraint<CancelOrderAssertions> BeCancelled(string because = "")
    {
        return new AndConstraint<CancelOrderAssertions>(this);
    }
}

public static class CancelOrderExtensions
{
    public static CancelOrderAssertions Should(this CancelOrder instance) => new(instance);
}

public class ShouldExtension
{
    public void Test(CancelOrder order)
    {
        order.Should().BeCancelled("it was refused");
    }
}
