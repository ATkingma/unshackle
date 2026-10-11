using System;
using FluentAssertions;

namespace Proofs;

public class MessageWildcardCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("*ship?ed*");
    }
}
