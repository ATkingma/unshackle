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
    private readonly Mock<IClock> _clock = new Mock<IClock>();

    public void Test()
    {
        var sut = new Service(_clock.Object);
    }
}
