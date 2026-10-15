using FluentAssertions;

namespace Proofs;

public class GenericCase
{
    public void Test<T>(T result)
    {
        result.Should().BeNull();
    }
}
