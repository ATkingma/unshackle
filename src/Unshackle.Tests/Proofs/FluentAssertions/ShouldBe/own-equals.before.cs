using FluentAssertions;

namespace Proofs;

// Fixed with a warning (TAAK-200): Shouldly calls expected.Equals(actual), FluentAssertions actual.Equals(expected).
public class Money
{
    public decimal Amount { get; init; }

    public override bool Equals(object? obj) => obj is Money other && other.Amount == Amount;

    public override int GetHashCode() => Amount.GetHashCode();
}

public class OwnEquals
{
    public void Test(Money actual, Money expected)
    {
        actual.Should().Be(expected);
    }
}