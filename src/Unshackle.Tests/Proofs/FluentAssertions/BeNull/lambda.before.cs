using System;
using FluentAssertions;

namespace Proofs;

public class Lambda
{
    public void Test(string? text)
    {
        Action act = () => text.Should().BeNull();
    }
}
