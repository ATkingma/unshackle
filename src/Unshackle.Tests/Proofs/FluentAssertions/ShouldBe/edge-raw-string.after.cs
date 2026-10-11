using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(string json)
    {
        json.ShouldBe("""{"name":"a"}""");
    }
}
