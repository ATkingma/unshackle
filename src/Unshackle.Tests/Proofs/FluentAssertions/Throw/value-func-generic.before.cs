using System;
using FluentAssertions;

namespace Proofs;

public class ValueFuncGenericCase<T>
{
    public void Test()
    {
        Func<T> local = () => throw new ArgumentException();
        local.Should().Throw<ArgumentException>();
    }
}
