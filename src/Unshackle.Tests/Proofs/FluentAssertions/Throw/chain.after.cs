using System;
using Shouldly;

namespace Proofs;

public class ChainCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).InnerException.ShouldNotBeNull().ShouldBeAssignableTo<ArgumentException>();
    }
}
