using System;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(DayOfWeek day)
    {
        day.ShouldBe(DayOfWeek.Monday);
    }
}
