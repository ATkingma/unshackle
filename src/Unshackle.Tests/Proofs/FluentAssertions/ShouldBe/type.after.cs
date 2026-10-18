using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(Type t, Type? n)
    {
        t.ShouldBe(typeof(List<int>));
        t.ShouldBe(typeof(List<int>), string.Format(System.Globalization.CultureInfo.InvariantCulture, "the {0} type", "list"));
        n.ShouldBe(typeof(int), "it is set");
    }
}
