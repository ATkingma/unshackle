using FluentAssertions;

namespace Proofs;

public class GenericClassCase
{
    public void Test<T>(T result) where T : class
    {
        result.Should().NotBeNull();
    }
}
