using System;
using Shouldly;

namespace Proofs;

public class ResultInnerExactlyCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).InnerException.ShouldBeOfType<ArgumentException>("the cause is kept");
    }
}
