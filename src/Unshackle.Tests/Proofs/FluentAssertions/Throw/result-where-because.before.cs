using System;
using FluentAssertions;

namespace Proofs;

public class ResultWhereBecauseCase
{
    public void Test(Action act)
    {
        act.Should().Throw<ArgumentException>().Where(e => e.ParamName == "id", "the id is checked");
    }
}
