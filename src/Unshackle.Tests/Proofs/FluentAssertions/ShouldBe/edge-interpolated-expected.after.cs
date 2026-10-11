using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(string url, string root)
    {
        url.ShouldBe($"{root}/license");
    }
}
