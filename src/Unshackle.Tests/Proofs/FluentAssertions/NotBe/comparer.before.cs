using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Comparer
{
    public void Test(object actual, object other)
    {
        actual.Should().NotBe(other, EqualityComparer<object>.Default);
    }
}
