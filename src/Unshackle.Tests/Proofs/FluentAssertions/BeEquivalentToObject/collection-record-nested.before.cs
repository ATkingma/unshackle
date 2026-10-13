using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public sealed record Inner(int Id);

public sealed record Outer(int Id, Inner Child);

public class Case
{
    public void Test(List<Outer> actual, Outer[] expected)
    {
        actual.Should().BeEquivalentTo(expected);
    }
}
