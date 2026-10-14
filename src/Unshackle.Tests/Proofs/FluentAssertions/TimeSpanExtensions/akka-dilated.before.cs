using System;
using FluentAssertions.Extensions;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): the TestKit scales timeouts with an own extension, 3.Seconds().Dilated().
public static class TestKitTime
{
    public static TimeSpan Dilated(this TimeSpan duration) => duration;
}

public class DilatedCase
{
    public TimeSpan Test() => 3.Seconds().Dilated();
}
