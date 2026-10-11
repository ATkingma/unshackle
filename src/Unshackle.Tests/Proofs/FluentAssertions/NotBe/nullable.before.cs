#nullable enable
using FluentAssertions;

namespace Proofs;

public class NullableSubject
{
    public void Test(int? count, string? name)
    {
        count.Should().NotBe(0);
        name.Should().NotBe("timme");
    }
}
