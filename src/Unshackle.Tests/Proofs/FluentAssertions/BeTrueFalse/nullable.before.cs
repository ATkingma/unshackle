using FluentAssertions;

namespace Proofs;

public class Nullable
{
    public void Test(bool? flag)
    {
        flag.Should().BeTrue();
        flag.Should().BeFalse("why");
    }
}
