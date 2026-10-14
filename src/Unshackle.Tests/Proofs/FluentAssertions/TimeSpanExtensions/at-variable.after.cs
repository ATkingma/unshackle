using System;
using Shouldly;

namespace Proofs;

public class AtVariableCase
{
    public DateTime Test(DateTime date) => new DateTime(date.Year, date.Month, date.Day, 9, 15, 30, 0, date.Kind);

    public DateTime WithMilliseconds(DateTime date) => new DateTime(date.Year, date.Month, date.Day, 9, 15, 30, 250, date.Kind);
}
