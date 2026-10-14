using System;
using FluentAssertions.Extensions;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): a timeout kept in a static field.
public class FieldInitializerCase
{
    private static readonly TimeSpan Timeout = 10.Seconds();

    public TimeSpan Remaining { get; } = 500.Milliseconds();

    public TimeSpan Get() => Timeout;
}
