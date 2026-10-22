using Moq;

namespace Proofs;

public interface IBus
{
    int Size { get; }
    void Send(int id, string text);
    int Publish(string topic);
}

public static class Probe
{
    public static string Run()
    {
        var bus = new Mock<IBus>();
        bus.Setup(b => b.Publish(It.IsAny<string>())).Returns(1).Verifiable();
        bus.Setup(b => b.Send(It.Is<int>(i => i > 0), "a")).Verifiable();
        bus.Setup(b => b.Size).Returns(3);
        bus.Object.Publish("t");
        bus.Object.Send(2, "a");
        _ = bus.Object.Size;
        bus.VerifyAll();
        return "verified";
    }
}
