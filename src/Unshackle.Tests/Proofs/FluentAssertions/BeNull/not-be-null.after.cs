using Shouldly;

namespace Proofs;

public class NotBeNullProof
{
    public void Test(object? item)
    {
        item.ShouldNotBeNull();
    }
}
