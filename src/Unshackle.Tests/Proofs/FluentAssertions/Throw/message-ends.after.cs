using System;
using Shouldly;

namespace Proofs;

public class MessageEndsCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).Message.ShouldEndWith("shipped", Shouldly.Case.Insensitive);
    }
}
