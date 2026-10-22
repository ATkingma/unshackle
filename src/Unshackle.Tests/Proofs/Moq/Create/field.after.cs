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
    private readonly IClock _clock = Substitute.For<IClock>();

    public void Test()
    {
        var sut = new Service(_clock);
    }
}
