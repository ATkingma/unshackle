using System;
using Shouldly;

namespace Proofs;

public class ResultExactlyInnerCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>().InnerException.ShouldNotBeNull().ShouldBeAssignableTo<ArgumentException>();
    }
}
