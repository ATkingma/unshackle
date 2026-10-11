using System;
using FluentAssertions;

namespace Proofs;

public class NamedBecauseCase
{
    public void Test(int x)
    {
        x.Should().BeGreaterThan(1, because: "it is positive");
    }
}
