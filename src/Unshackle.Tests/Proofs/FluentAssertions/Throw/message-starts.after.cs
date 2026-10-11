using System;
using Shouldly;

namespace Proofs;

public class MessageStartsCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).Message.ShouldStartWith("Order", Shouldly.Case.Insensitive);
    }
}
