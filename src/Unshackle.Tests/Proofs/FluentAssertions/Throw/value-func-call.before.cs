using System;
using FluentAssertions;

namespace Proofs;

public class ValueFuncCallCase
{
    public void Test(int seed)
    {
        CallingWith(seed).Should().Throw<ArgumentException>();
    }

    private static Func<int> CallingWith(int seed) => () => throw new ArgumentException();
}
