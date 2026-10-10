using Shouldly;

namespace Proofs;

public class Because
{
    public void Test(string? text)
    {
        text.ShouldBeNull("not set yet");
        text.ShouldNotBeNull("must be set");
    }
}
