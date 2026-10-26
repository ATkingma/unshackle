using System;
using Moq;
using Moq.Protected;

namespace Proofs;

public interface IFeed
{
    int Next();

    event EventHandler Changed;
}

public class Handler
{
    protected virtual int Compute() => 1;
}

public class UnsupportedProof
{
    public void Test()
    {
        var handler = new Mock<Handler>();
        handler.Protected().Setup<int>("Compute").Returns(2);
        var sut = handler.Object;
    }
}
