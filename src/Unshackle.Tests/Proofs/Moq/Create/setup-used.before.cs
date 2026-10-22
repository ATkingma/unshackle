using Moq;

namespace Proofs;

public interface IClock
{
    System.DateTime Now { get; }
}

public class Service
{
    public Service(IClock clock)
    {
        Clock = clock;
    }

    public IClock Clock { get; }
}

public class CreateProof
{
    public void Test()
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.Now).Returns(System.DateTime.MinValue);
        var sut = new Service(clock.Object);
    }
}
