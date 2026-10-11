#nullable enable
using Shouldly;

namespace Proofs;

public class NullableSubject
{
    public void Test(int? count, string? name)
    {
        count.ShouldNotBe(0);
        name.ShouldNotBe("timme");
    }
}
