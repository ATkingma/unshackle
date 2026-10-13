using System;
using FluentAssertions;

namespace Proofs;

public class ResultWhereCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().Where(e => e.Message.Length > 0);
    }
}
