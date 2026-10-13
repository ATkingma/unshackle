using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class Case
{
    public TimeSpan Test(TimeSpan a, TimeSpan b) => a.And(b);
}
