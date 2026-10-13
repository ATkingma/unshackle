using System;
using FluentAssertions;

namespace Proofs;

public class ResultLocalPassedCase
{
    public void Test(Action act)
    {
        var ex = act.Should().Throw<ArgumentException>();
        Inspect(ex);
    }

    private static void Inspect(FluentAssertions.Specialized.ExceptionAssertions<ArgumentException> assertions)
    {
    }
}
