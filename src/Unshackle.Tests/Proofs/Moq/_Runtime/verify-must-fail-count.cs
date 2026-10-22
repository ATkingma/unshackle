// Expect: VerifyFailed
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
        new Sender(bus.Object).Run(2);
        bus.Verify(b => b.Send(It.IsAny<int>(), It.IsAny<string>()), Times.Once());
        return "verified";
    }
}
