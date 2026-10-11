using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class StringCase
{
    public void Test(string text)
    {
        text.ShouldContain("ab", Shouldly.Case.Sensitive);
    }
}
