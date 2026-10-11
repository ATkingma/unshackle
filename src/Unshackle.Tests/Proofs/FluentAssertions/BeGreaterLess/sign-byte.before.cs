using System;
using FluentAssertions;

namespace Proofs;

public class SignByteCase
{
    private int Next() => 1;

    public void Test(byte b)
    {
        b.Should().BePositive();
    }
}
