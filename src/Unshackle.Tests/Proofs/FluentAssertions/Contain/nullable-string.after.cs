#nullable enable
using Shouldly;

namespace Proofs;

public class NullableContain
{
    public void Test(string? text)
    {
        text.ShouldNotBeNull().ShouldContain("ab", Shouldly.Case.Sensitive);
    }
}
