using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class Case
{
    public TimeSpan Test() => FluentTimeSpanExtensions.Seconds(5);
}
