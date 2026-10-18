using System;
using FluentAssertions;

namespace Proofs;

public class InvokingLocalCase
{
    private readonly InvokingLocalCase _other = new();

    public void Fail(int value) => throw new ArgumentOutOfRangeException(nameof(value));

    public int Get(int value) => throw new ArgumentOutOfRangeException(nameof(value));

    public void Test()
    {
        var act = _other.Invoking(x => x.Fail(1));
        var withResult = _other.Invoking(x => x.Get(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
        withResult.Should().Throw<ArgumentOutOfRangeException>();
    }
}
