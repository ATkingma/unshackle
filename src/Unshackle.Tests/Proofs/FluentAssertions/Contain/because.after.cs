using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class BecauseCase
{
    public void Test(List<int> x, string text)
    {
        x.ShouldContain(2, "it was added");
        text.ShouldContain("ab", Shouldly.Case.Sensitive, "the prefix");
    }
}
