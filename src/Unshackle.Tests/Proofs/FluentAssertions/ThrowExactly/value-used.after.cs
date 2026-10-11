using System;
using Shouldly;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(Action act)
    {
        string message = Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>().Message;
    }
}
