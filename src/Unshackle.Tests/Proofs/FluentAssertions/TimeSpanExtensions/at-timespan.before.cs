using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class AtTimeSpanCase
{
    public string Test(DateTime date, TimeSpan time) => date.At(time).ToString("o");

    public DateTime Month(TimeSpan time) => 1.March(2020).At(time);
}
