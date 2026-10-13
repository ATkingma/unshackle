using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class Case
{
    public DateTime Test(int day, int year) => day.March(year);
}
