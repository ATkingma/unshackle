using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test<T>(T value, T expected)
    {
        value.ShouldBe(expected);
    }
}
