using Moq;
using NSubstitute;

namespace Proofs;

public interface IBus
{
    int Size { get; }
    void Send(int id, string text);
    int Publish(string topic);
}

public class VerifyProof
{
    public void Test()
    {
        var bus = new Mock<IBus>();
        bus.Object.Send(1, "a");
        bus.Object.Received(1).Send(Arg.Any<int>(), Arg.Any<string>());
    }
}
