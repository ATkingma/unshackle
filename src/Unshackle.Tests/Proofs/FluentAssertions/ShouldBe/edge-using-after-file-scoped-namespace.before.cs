namespace Proofs;

using FluentAssertions;

public class UsingAfterFileScopedNamespace
{
    public void Test(int result)
    {
        result.Should().Be(42);
    }
}
