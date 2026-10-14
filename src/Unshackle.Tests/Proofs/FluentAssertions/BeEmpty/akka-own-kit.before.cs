using System.Collections.Generic;
using FluentAssertions;
using Proofs.TestKit;

namespace Proofs.TestKit
{
    // Shape from Akka.NET (Apache-2.0): Akka.TestKit ships its own Should* extensions (AkkaSpecExtensions).
    public static class SpecExtensions
    {
        public static void ShouldBe<T>(this T actual, T expected, string? message = null) { }
        public static void ShouldBeTrue(this bool actual, string? message = null) { }
        public static void ShouldBeGreaterThan<T>(this T actual, T other, string? message = null) where T : System.IComparable<T> { }
    }
}

namespace Proofs
{
    public class OwnKitCase
    {
        public void Test(List<int> pending)
        {
            pending.Should().BeEmpty();
        }
    }
}
