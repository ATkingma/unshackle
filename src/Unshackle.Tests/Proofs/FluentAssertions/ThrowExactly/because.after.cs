using System;
using Shouldly;

namespace Proofs;

public class BecauseCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act, "it is shipped").ShouldBeOfType<InvalidOperationException>();
    }
}
