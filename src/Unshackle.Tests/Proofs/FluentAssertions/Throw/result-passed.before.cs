using System;
using FluentAssertions;

namespace Proofs;

public class ResultPassedCase
{
    public void Test(Action act)
    {
        Inspect(act.Should().Throw<ArgumentException>());
    }

    private static void Inspect(FluentAssertions.Specialized.ExceptionAssertions<ArgumentException> assertions)
    {
    }
}
