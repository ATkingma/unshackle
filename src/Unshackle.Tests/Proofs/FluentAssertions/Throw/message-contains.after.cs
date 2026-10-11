using System;
using Shouldly;

namespace Proofs;

public class MessageContainsCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("shipped", Shouldly.Case.Insensitive);
    }
}
