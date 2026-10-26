using System;
using Moq;


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
        var feed = new Mock<IFeed>();
        int calls = feed.Invocations.Count;
        var sut = feed.Object;
    }
}
