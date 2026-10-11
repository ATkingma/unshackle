using FluentAssertions;

namespace Proofs;

public record Money(decimal Amount, string Currency);

public class Records
{
    public void Test(Money price)
    {
        price.Should().NotBe(new Money(0m, "EUR"));
    }
}
