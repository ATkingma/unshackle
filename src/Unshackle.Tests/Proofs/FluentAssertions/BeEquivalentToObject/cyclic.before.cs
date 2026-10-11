using FluentAssertions;

namespace Proofs;

public class Node
{
    public Node Parent { get; set; }
}

public class Case
{
    public void Test(Node a, Node b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
