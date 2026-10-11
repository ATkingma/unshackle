using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using SampleShop.Domain;

namespace SampleShop.Advanced.Tests;

// Custom assertion extension: user code built on FluentAssertions' extension model.
public static class OrderAssertionExtensions
{
    public static OrderAssertions Should(this Order instance) => new(instance);
}

public class OrderAssertions : ReferenceTypeAssertions<Order, OrderAssertions>
{
    public OrderAssertions(Order instance)
        : base(instance)
    {
    }

    protected override string Identifier => "order";

    public AndConstraint<OrderAssertions> BeCancelled(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Status == OrderStatus.Cancelled)
            .FailWith("Expected {context:order} to be cancelled{reason}, but it is {0}.", Subject.Status);

        return new AndConstraint<OrderAssertions>(this);
    }
}
