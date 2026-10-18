using System;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class InvokingLocalCase
{
    private readonly InvokingLocalCase _other = new();

    public void Fail(int value) => throw new ArgumentOutOfRangeException(nameof(value));

    public int Get(int value) => throw new ArgumentOutOfRangeException(nameof(value));

    public void Test()
    {
        Action act = () => _other.Fail(1);
        Func<int> withResult = () => _other.Get(1);

        act.Should().Throw<ArgumentOutOfRangeException>();
        withResult.Should().Throw<ArgumentOutOfRangeException>();
    }
}
