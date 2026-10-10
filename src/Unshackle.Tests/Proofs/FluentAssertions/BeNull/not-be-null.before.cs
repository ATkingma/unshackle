using FluentAssertions;

namespace Proofs;

public class NotBeNullProof
{
    public void Test(object? item)
    {
        item.Should().NotBeNull();
    }
}
