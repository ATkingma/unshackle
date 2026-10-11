using System;
using Shouldly;

namespace Proofs;

public class MessageCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>().Message.ShouldStartWith("Order", Shouldly.Case.Insensitive);
    }
}
