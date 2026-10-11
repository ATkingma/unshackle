using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class GenericCase
{
    public void Test(Action act)
    {
        act.Should().NotThrow<InvalidOperationException>();
    }
}
