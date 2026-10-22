// Expect: VerifyFailed
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
        bus.Setup(b => b.Send(1, "a")).Verifiable();
        bus.Object.Send(1, "b");
        bus.Verify();
        return "verified";
    }
}
