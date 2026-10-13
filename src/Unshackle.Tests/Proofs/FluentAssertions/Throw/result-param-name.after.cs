using System;
using Shouldly;

namespace Proofs;

public class ResultParamNameCase
{
    public void Test(Action act)
    {
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("id");
    }
}
