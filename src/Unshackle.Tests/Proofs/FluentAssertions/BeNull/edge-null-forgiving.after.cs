using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(string? text)
    {
        text!.ShouldNotBeNull();
    }
}
