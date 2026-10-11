using Shouldly;

namespace Proofs;

public record Money(decimal Amount, string Currency);

public class Records
{
    public void Test(Money price)
    {
        price.ShouldNotBe(new Money(0m, "EUR"));
    }
}
