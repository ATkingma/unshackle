using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(string text, int id)
    {
        text.ShouldContain($"id={id}", Shouldly.Case.Sensitive);
    }
}
