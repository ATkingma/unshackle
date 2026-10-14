using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class AtComputedCase
{
    public DateTime Next() => DateTime.Today;

    public DateTime Test() => Next().At(6, 0);
}
