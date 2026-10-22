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
        bus.Setup(b => b.Publish("t")).Returns(1).Verifiable();
        bus.Setup(b => b.Size).Returns(3);
        bus.Object.Publish("t");
        bus.Verify();
        return "verified";
    }
}
