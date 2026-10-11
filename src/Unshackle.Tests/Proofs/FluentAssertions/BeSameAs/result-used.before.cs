using FluentAssertions;
using System;

namespace Proofs;

public class Case
{
    public void Test(object a, object b)
    {
        var c = a.Should().BeSameAs(b).And;
    }
}
