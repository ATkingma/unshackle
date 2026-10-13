using Proofs.OwnKit;
using static Shouldly.ShouldBeBooleanExtensions;

namespace Proofs.OwnKit
{
    // The project's own assertions: with `using Shouldly;` every ShouldBe call would be ambiguous (CS0121, TAAK-198).
    public static class SpecExtensions
    {
        public static void ShouldBe<T>(this T actual, T expected, string? message = null) { }
    }
}

namespace Proofs
{
    public class OwnShouldBeInScope
    {
        public void Test(int total, bool done)
        {
            done.ShouldBeTrue();
            total.ShouldBe(25);
        }
    }
}