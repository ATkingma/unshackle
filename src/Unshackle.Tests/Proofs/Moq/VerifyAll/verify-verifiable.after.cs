using Moq;
using NSubstitute;

namespace Proofs;

public interface IBus
{
    int Size { get; }
    void Send(int id, string text);
    int Publish(string topic);
}

public class VerifyAllProof
{
    public void Test()
    {
        var bus = new Mock<IBus>();
        bus.Setup(b => b.Publish(It.IsAny<string>())).Returns(1).Verifiable();
        bus.Setup(b => b.Send(1, "a")).Verifiable();
        bus.Setup(b => b.Size).Returns(3);
        bus.Object.Send(1, "a");
        bus.Object.Received().Publish(Arg.Any<string>());
        bus.Object.Received().Send(1, "a");
    }
}
