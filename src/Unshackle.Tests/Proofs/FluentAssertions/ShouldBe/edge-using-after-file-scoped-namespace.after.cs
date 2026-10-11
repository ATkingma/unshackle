namespace Proofs;

using Shouldly;

public class UsingAfterFileScopedNamespace
{
    public void Test(int result)
    {
        result.ShouldBe(42);
    }
}
