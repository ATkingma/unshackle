#nullable enable
using System;
using FluentAssertions;

namespace Proofs;

public class ValueFuncNullableCase
{
    public void Test(Func<int>? func)
    {
        func.Should().Throw<ArgumentException>();
    }
}
