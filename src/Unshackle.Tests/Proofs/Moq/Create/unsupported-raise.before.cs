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
        feed.Raise(f => f.Changed += null, EventArgs.Empty);
        var sut = feed.Object;
    }
}
