using Moq;

namespace Proofs;

public interface IBus
{
    int Size { get; }
    void Send(int id, string text);
    int Publish(string topic);
}

public class Sender
{
    private readonly IBus _bus;

    public Sender(IBus bus)
    {
        _bus = bus;
    }

    public void Run(int times)
    {
        for (int i = 0; i < times; i++)
        {
            _bus.Send(i, "t" + i);
        }

        _ = _bus.Size;
    }
}

public static class Probe
{
    public static string Run()
    {
        var bus = new Mock<IBus>();
        bus.Setup(b => b.Publish(It.IsAny<string>())).Returns(4);
        int published = bus.Object.Publish("x");
        bus.Verify(b => b.Publish("x"), Times.Once());
        bus.Verify(b => b.Publish("y"), Times.Never());
        return "published " + published;
    }
}
