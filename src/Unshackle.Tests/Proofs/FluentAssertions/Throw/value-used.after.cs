using System;
using Shouldly;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(Action act)
    {
        var exception = Should.Throw<InvalidOperationException>(act);
    }
}
