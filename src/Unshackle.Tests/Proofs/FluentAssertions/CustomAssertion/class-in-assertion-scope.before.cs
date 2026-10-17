using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Proofs;

public class Parcel
{
    public bool Delivered { get; set; }
}

public static class ParcelAssertionExtensions
{
    public static ParcelAssertions Should(this Parcel instance) => new(instance);
}

public class ParcelAssertions : ReferenceTypeAssertions<Parcel, ParcelAssertions>
{
    public ParcelAssertions(Parcel instance)
        : base(instance)
    {
    }

    protected override string Identifier => "parcel";

    public AndConstraint<ParcelAssertions> BeDelivered()
    {
        Execute.Assertion
            .ForCondition(Subject.Delivered)
            .FailWith("Expected {context} to be delivered.");

        return new AndConstraint<ParcelAssertions>(this);
    }
}

public class ClassInAssertionScope
{
    public void Test(Parcel first, Parcel second)
    {
        // Inside a scope the first failure does not throw; ShouldBeDelivered would, so no use converts and neither does the class.
        using (new AssertionScope())
        {
            first.Should().BeDelivered();
            second.Should().BeDelivered();
        }
    }
}
