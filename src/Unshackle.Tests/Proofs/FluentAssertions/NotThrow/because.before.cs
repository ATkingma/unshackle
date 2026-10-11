using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class BecauseCase
{
    public void Test(Action act)
    {
        act.Should().NotThrow("it is valid");
    }
}
