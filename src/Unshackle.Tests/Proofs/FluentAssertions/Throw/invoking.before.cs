using System;
using FluentAssertions;

namespace Proofs;

public class InvokingCase
{
    public void Test(Action act, string text)
    {
        text.Invoking(t => t.ToString()).Should().Throw<InvalidOperationException>();
    }
}
