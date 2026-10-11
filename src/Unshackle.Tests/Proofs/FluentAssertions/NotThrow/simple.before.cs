using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class SimpleCase
{
    public void Test(Action act)
    {
        act.Should().NotThrow();
    }
}
