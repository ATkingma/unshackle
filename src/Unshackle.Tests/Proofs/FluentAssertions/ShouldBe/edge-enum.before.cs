using System;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(DayOfWeek day)
    {
        day.Should().Be(DayOfWeek.Monday);
    }
}
