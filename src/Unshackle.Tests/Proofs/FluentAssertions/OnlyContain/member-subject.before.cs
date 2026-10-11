using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public List<int> Items { get; } = new();

    public void Test(Case other)
    {
        other.Items.Should().OnlyContain(i => i > 0);
        this.Items.Should().OnlyContain(i => i > 0);
    }
}
