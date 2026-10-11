using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class NamedBecauseCase
{
    public void Test(List<int> x)
    {
        x.Should().Contain(2, because: "it was added");
    }
}
