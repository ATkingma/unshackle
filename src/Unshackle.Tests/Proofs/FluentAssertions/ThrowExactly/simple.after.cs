using System;
using Shouldly;

namespace Proofs;

public class SimpleCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>();
    }
}
