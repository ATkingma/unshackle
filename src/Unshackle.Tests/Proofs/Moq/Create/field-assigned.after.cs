using NSubstitute;

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
    private IClock _clock;

    public CreateProof()
    {
        _clock = Substitute.For<IClock>();
    }

    public Service Create() => new Service(this._clock);
}
