using System;
using FluentAssertions.Extensions;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): timeouts built from several units.
public class ArithmeticCase
{
    public TimeSpan Test(bool slow) => slow ? 1.Minutes() + 30.Seconds() : 250.Milliseconds();
}
