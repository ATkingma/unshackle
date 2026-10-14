using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

// Shape from EventFlow (MIT): Voyages.cs builds schedules with 1.October(2008).At(12, 00) and has no using System.
public class MonthAtCase
{
    public List<object> Departures() => [new DateTime(2008, 10, 1, 12, 00, 0), new DateTime(2008, 10, 3, 14, 30, 0)];
}
