using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int age, string name)
    {
        age.Should().BeLessThan(150, $"{name} is human");
    }
}
