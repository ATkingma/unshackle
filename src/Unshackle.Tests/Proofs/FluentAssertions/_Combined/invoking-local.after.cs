using System;
using Shouldly;

namespace Proofs;

public class InvokingLocalCombinedCase
{
    private readonly InvokingLocalCombinedCase _other = new();

    public void Fail(int value) => throw new ArgumentOutOfRangeException(nameof(value));

    public int Get(int value) => throw new ArgumentOutOfRangeException(nameof(value));

    public void Test()
    {
        Action act = () => _other.Fail(1);
        Func<int> withResult = () => _other.Get(1);

        Should.Throw<ArgumentOutOfRangeException>(act);
        Should.Throw<ArgumentOutOfRangeException>(() => withResult());
    }
}
