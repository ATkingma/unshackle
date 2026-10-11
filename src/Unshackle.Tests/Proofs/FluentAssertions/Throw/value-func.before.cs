using System;
using FluentAssertions;

namespace Proofs;

public class ValueFuncCase
{
    public void Test(Action act, Func<int> func)
    {
        func.Should().Throw<InvalidOperationException>();
    }
}
