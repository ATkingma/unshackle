using Shouldly;
using System;

namespace Proofs;

public class Case
{
    public void Test(object a, object b)
    {
        a.ShouldNotBeNull();
        a.ShouldBeSameAs(b);
    }
}
