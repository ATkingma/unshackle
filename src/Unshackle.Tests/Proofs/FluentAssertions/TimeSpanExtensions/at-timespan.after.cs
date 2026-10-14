using System;
using Shouldly;

namespace Proofs;

public class AtTimeSpanCase
{
    public string Test(DateTime date, TimeSpan time) => (date.Date + time).ToString("o");

    public DateTime Month(TimeSpan time) => new DateTime(2020, 3, 1) + time;
}
