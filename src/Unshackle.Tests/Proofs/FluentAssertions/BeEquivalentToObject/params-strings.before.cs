using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> names, List<string?> maybe)
    {
        names.Should().BeEquivalentTo("A", "B");
        names.Should().BeEquivalentTo("A");
        maybe.Should().BeEquivalentTo("A", null);
        names.Should().BeEquivalentTo();
    }
}
