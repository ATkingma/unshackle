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
    private readonly Mock<IBus> _bus = new Mock<IBus>();

    public void Test()
    {
        _bus.Setup(b => b.Publish("x")).Returns(2);
        _bus.VerifyAll();
    }
}
