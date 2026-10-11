using Shouldly;

namespace Proofs;

public class String
{
    public void Test(string text)
    {
        text.ShouldBeEmpty();
        text.ShouldNotBeEmpty();
    }
}
