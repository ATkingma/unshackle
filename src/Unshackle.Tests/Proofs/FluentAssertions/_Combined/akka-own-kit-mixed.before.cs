using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Extensions;
using Proofs.TestKit;

namespace Proofs.TestKit
{
    // Shape from Akka.NET (Apache-2.0): Akka.TestKit ships its own ShouldBe; only the calls that need it stay FluentAssertions (TAAK-198).
    public static class SpecExtensions
    {
        public static void ShouldBe<T>(this T actual, T expected, string? message = null) { }
    }
}

namespace Proofs
{
    public class OwnKitMixedCase
    {
        public void Test(int count, object? reply, List<string> members, Action tell)
        {
            count.Should().Be(3);
            reply.Should().NotBeNull();
            members.Should().Contain("node-1");
            tell.Should().Throw<InvalidOperationException>();
            Await(3.Seconds());
            count.ShouldBe(3);
        }

        private static void Await(TimeSpan timeout)
        {
        }
    }
}
