using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class Case
{
    public void Wait(TimeSpan timeout)
    {
    }

    public void Test(int n)
    {
        Wait(3.Seconds());
        Wait(200.Milliseconds());
        Wait(n.Minutes());
        Wait(2.Hours());
        Wait(1.Days());
        Wait(10.Ticks());
        Wait(0.5.Seconds());
        Wait((n * 2).Seconds());
        TimeSpan negative = -1.Seconds();
    }
}
