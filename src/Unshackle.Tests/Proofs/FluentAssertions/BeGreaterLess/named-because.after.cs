using System;
using Shouldly;

namespace Proofs;

public class NamedBecauseCase
{
    public void Test(int x)
    {
        x.ShouldBeGreaterThan(1, "it is positive");
    }
}
