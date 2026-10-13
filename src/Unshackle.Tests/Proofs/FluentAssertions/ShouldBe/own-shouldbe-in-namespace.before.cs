using FluentAssertions;

namespace Proofs.TestKit
{
    // The project's own assertion: in its own namespace it wins over `using Shouldly;` (no ambiguity, TAAK-199).
    public static class SpecExtensions
    {
        public static void ShouldBe<T>(this T actual, T expected, string? message = null) { }
    }

    public class OwnShouldBeInNamespace
    {
        public void Test(int total, int items)
        {
            total.Should().Be(25, "{0} items", items);
        }
    }
}
