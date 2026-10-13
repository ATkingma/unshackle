using System;
using Shouldly;

namespace Proofs;

public class ResultWhereBecauseCase
{
    public void Test(Action act)
    {
        var exception = Should.Throw<ArgumentException>(act);
        (exception.ParamName == "id").ShouldBeTrue("the id is checked");
    }
}
