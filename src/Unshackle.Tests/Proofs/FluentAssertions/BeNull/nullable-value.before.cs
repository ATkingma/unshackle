using FluentAssertions;

namespace Proofs;

public class NullableValue
{
    public void Test(int? number, bool? flag)
    {
        number.Should().BeNull();
        flag.Should().NotBeNull("why");
    }
}
