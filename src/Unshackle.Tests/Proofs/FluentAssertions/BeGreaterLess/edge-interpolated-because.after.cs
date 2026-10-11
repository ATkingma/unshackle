using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int age, string name)
    {
        age.ShouldBeLessThan(150, $"{name} is human");
    }
}
