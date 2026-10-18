using System;
using Shouldly;

namespace Proofs;

public class ValueFuncCallCase
{
    public void Test(int seed)
    {
        var subject = CallingWith(seed);
        Should.Throw<ArgumentException>(() => subject());
    }

    private static Func<int> CallingWith(int seed) => () => throw new ArgumentException();
}
