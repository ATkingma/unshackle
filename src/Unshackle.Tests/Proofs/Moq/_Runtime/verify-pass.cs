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
        new Sender(bus.Object).Run(3);
        bus.Verify(b => b.Send(0, "t0"), Times.Once());
        bus.Verify(b => b.Send(It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(3));
        bus.Verify(b => b.Send(It.Is<int>(i => i > 0), It.IsAny<string>()), Times.Exactly(2));
        bus.Verify(b => b.Send(5, It.IsAny<string>()), Times.Never);
        bus.Verify(b => b.Publish(It.IsAny<string>()), Times.Never());
        bus.Verify(b => b.Send(1, It.IsAny<string>()));
        bus.Verify(b => b.Send(2, "t2"), Times.AtLeastOnce());
        bus.Verify(b => b.Size, Times.Once());
        return "verified";
    }
}
