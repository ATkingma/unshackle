using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class Case
{
    public TimeSpan Test(TimeSpan offset) => FluentTimeSpanExtensions.Seconds(5, offset);
}
