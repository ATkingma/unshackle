using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(string json)
    {
        json.Should().Be("""{"name":"a"}""");
    }
}
