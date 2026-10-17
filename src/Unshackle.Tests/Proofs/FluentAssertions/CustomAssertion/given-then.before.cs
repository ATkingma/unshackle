using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Proofs;

public class Invoice
{
    public int Total { get; set; }
}

public static class InvoiceAssertionExtensions
{
    public static InvoiceAssertions Should(this Invoice instance) => new(instance);
}

public class InvoiceAssertions : ReferenceTypeAssertions<Invoice, InvoiceAssertions>
{
    public InvoiceAssertions(Invoice instance)
        : base(instance)
    {
    }

    protected override string Identifier => "invoice";

    public AndConstraint<InvoiceAssertions> BePaid()
    {
        Execute.Assertion
            .Given(() => Subject.Total)
            .ForCondition(total => total == 0)
            .FailWith("Expected {context} to be paid, but {0} is open.", total => total);

        return new AndConstraint<InvoiceAssertions>(this);
    }
}

public class GivenThen
{
    public void Test(Invoice invoice)
    {
        invoice.Should().BePaid();
    }
}
