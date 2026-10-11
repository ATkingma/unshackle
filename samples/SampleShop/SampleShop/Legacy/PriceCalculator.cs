namespace SampleShop.Legacy;

// Roadmap step 6 (generic search-and-replace): an obsolete API with callers to rewrite.
public static class PriceCalculator
{
    [Obsolete("Use CalculateWithVat(amount, rate) instead.")]
    public static decimal AddVat(decimal amount) => CalculateWithVat(amount, 0.21m);

    public static decimal CalculateWithVat(decimal amount, decimal rate) => Math.Round(amount * (1 + rate), 2);
}
