using FluentAssertions;

namespace Proofs;

public class Simple
{
    public void Test(int result, string name)
    {
        result.Should().NotBe(41);
        name.Should().NotBe("timme");
    }
}
