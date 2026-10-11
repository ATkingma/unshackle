using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(Action act)
    {
        act.Should().NotThrow("because {0}", 1);
    }
}
