using Shouldly;
using System;

namespace Proofs;

public class Case
{
    public void Test(Type t, Type? n)
    {
        t.ShouldNotBe(typeof(string));
        n.ShouldNotBe(typeof(int));
    }
}
