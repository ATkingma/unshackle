using System;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(Action act) => act.Should().NotThrow();
}
