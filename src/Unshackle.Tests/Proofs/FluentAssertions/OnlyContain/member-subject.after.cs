using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public List<int> Items { get; } = new();

    public void Test(Case other)
    {
        other.Items.ShouldNotBeEmpty();
        other.Items.ShouldAllBe(i => i > 0);
        this.Items.ShouldNotBeEmpty();
        this.Items.ShouldAllBe(i => i > 0);
    }
}
