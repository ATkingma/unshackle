using System;
using FluentAssertions;

namespace Proofs;

public class FuncCase
{
    public void Test(Action act, Func<string> func)
    {
        func.Should().Throw<InvalidOperationException>();
    }
}
