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
    private Mock<IClock> _clock;

    public CreateProof()
    {
        _clock = new Mock<IClock>();
    }

    public Service Create() => new Service(this._clock.Object);
}
