using System;
using Shouldly;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): a timeout kept in a static field.
public class FieldInitializerCase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public TimeSpan Remaining { get; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan Get() => Timeout;
}
