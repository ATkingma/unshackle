using System;
using FluentAssertions;

namespace Proofs;

public class LambdaCase
{
    public void Test(int x)
    {
        Action a = () => x.Should().BeGreaterThan(1);
    }
}
