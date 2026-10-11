using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(object value)
    {
        (value is string { Length: > 0 }).ShouldBeTrue();
    }
}
