using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(string text, string name)
    {
        text.ShouldBe($"hello {name}", StringCompareShould.IgnoreCase);
    }
}
