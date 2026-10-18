using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class NamedBecauseCase
{
    public void Test(List<int> x)
    {
        x.ShouldContain(2, "it was added");
    }
}
