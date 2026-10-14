using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Extensions;
using Proofs.TestKit;
using static Shouldly.ShouldBeEnumerableTestExtensions;
using static Shouldly.ShouldBeNullExtensions;
using Should = Shouldly.Should;

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
            reply.ShouldNotBeNull();
            members.ShouldContain("node-1");
            Should.Throw<InvalidOperationException>(tell);
            Await(TimeSpan.FromSeconds(3));
            count.ShouldBe(3);
        }

        private static void Await(TimeSpan timeout)
        {
        }
    }
}
