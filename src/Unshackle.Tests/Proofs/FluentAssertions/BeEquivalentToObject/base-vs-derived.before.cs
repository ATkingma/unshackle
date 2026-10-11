using FluentAssertions;

namespace Proofs;

public class Base
{
    public int X { get; set; }
}

public class Derived : Base
{
    public int Extra { get; set; }
}

public class Case
{
    public void Test(Base a, Derived b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
