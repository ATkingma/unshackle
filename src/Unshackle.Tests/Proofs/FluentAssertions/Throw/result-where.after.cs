using System;
using Shouldly;

namespace Proofs;

public class ResultWhereCase
{
    public void Test(Action act)
    {
        var exception = Should.Throw<InvalidOperationException>(act);
        (exception.Message.Length > 0).ShouldBeTrue();
    }
}
