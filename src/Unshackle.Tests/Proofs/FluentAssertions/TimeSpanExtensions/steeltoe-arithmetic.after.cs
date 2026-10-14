using System;
using Shouldly;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): timeouts built from several units.
public class ArithmeticCase
{
    public TimeSpan Test(bool slow) => slow ? TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(30) : TimeSpan.FromMilliseconds(250);
}
