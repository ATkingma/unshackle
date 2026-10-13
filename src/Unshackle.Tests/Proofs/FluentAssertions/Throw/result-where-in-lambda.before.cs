using System;
using FluentAssertions;

namespace Proofs;

public class ResultWhereInLambdaCase
{
    public void Test(Action act)
    {
        Action check = () => act.Should().Throw<ArgumentException>().Where(e => e.ParamName == "id");
        check();
    }
}
