using FluentAssertions;
using System;

namespace Proofs;

public class Case
{
    public void Test(object a, object b)
    {
        a.Should().NotBeNull().And.BeSameAs(b);
    }
}
