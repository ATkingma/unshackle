using Moq;

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
        bus.Setup(b => b.Publish(It.IsAny<string>())).Returns(1);
        bus.Setup(b => b.Publish("x")).Returns(2);
        bus.VerifyAll();
    }
}
