using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public DateTime Test(int year) => new DateTime(year, 3, 1);
}
