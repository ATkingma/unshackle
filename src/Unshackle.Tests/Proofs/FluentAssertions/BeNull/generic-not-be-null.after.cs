using Shouldly;

namespace Proofs;

public class GenericCase
{
    public void Test<T>(T result)
    {
        ((object)result).ShouldNotBeNull();
    }
}
