using Shouldly;

namespace Proofs;

public class BeNullProof
{
    public void Test(string? text)
    {
        text.ShouldBeNull();
    }
}
