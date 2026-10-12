using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string[] x)
    {
        x.ShouldContain("abc");
    }
}
