using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class BecauseCase
{
    public void Test(List<int> x, string text)
    {
        x.Should().Contain(2, "it was added");
        text.Should().Contain("ab", "the prefix");
    }
}
