using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public void Test(int x)
    {
        using (new AssertionScope())
        {
            System.Console.WriteLine(x);
            x.Should().Be(1);
        }
    }
}
