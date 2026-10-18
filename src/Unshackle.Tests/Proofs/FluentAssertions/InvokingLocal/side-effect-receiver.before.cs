using System;
using FluentAssertions;

namespace Proofs;

public class InvokingLocalSideEffectCase
{
    public InvokingLocalSideEffectCase Create() => new();

    public void Fail() => throw new InvalidOperationException();

    public void Test()
    {
        var act = Create().Invoking(x => x.Fail());
        act.Should().Throw<InvalidOperationException>();
    }
}
