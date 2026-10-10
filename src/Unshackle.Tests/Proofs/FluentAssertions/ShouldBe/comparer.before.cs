using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Comparer
{
    public void Test(object actual, object expected)
    {
        actual.Should().Be(expected, EqualityComparer<object>.Default);
    }
}
