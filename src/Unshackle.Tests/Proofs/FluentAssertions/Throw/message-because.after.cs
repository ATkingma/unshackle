using System;
using Shouldly;

namespace Proofs;

public class MessageBecauseCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act, "a").Message.ShouldContain("shipped", Shouldly.Case.Insensitive, "b");
    }
}
