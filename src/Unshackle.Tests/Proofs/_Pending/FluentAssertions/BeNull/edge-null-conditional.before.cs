using System;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(Uri? uri)
    {
        uri?.Host.Should().BeNull();
    }
}
