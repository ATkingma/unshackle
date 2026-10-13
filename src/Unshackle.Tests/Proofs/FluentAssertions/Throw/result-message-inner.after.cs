using System;
using Shouldly;

namespace Proofs;

public class ResultMessageInnerCase
{
    public void Test(Action act)
    {
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldBe("Order shipped", Shouldly.StringCompareShould.IgnoreCase);
        exception.InnerException.ShouldNotBeNull().ShouldBeAssignableTo<ArgumentException>();
    }
}
