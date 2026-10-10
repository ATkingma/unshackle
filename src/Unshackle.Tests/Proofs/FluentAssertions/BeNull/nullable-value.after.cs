using Shouldly;

namespace Proofs;

public class NullableValue
{
    public void Test(int? number, bool? flag)
    {
        number.ShouldBeNull();
        flag.ShouldNotBeNull("why");
    }
}
