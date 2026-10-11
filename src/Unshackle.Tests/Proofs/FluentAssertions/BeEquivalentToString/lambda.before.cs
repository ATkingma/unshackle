using System;
using FluentAssertions;

namespace Proofs;

public class LambdaCase
{
    public void Test(string s)
    {
        Action act = () => s.Should().BeEquivalentTo("ab");
    }
}
