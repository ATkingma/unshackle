using FluentAssertions;

namespace Proofs;

public class A
{
    public int X { get; set; }
}

public class B
{
    public int X { get; set; }
}

public class Case
{
    public void Test(A a, B b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
