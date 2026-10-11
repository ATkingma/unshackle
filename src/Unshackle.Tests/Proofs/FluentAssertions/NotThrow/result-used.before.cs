using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ResultUsedCase
{
    public void Test(Action act)
    {
        var result = act.Should().NotThrow();
    }
}
