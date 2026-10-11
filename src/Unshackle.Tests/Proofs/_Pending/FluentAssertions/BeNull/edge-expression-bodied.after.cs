using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(object? value) => value.ShouldBeNull();
}
