using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Wait(TimeSpan timeout)
    {
    }

    public void Test(int n)
    {
        Wait(TimeSpan.FromSeconds(3));
        Wait(TimeSpan.FromMilliseconds(200));
        Wait(TimeSpan.FromMinutes(n));
        Wait(TimeSpan.FromHours(2));
        Wait(TimeSpan.FromDays(1));
        Wait(TimeSpan.FromTicks(10));
        Wait(TimeSpan.FromSeconds(0.5));
        Wait(TimeSpan.FromSeconds(n * 2));
        TimeSpan negative = -TimeSpan.FromSeconds(1);
    }
}
