using System.Collections.Generic;
using FluentAssertions.Extensions;

namespace Proofs;

// Shape from EventFlow (MIT): Voyages.cs builds schedules with 1.October(2008).At(12, 00) and has no using System.
public class MonthAtCase
{
    public List<object> Departures() => [1.October(2008).At(12, 00), 3.October(2008).At(14, 30)];
}
