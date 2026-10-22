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
    public void Test()
    {
        IClock clock = Substitute.For<IClock>();
        var sut = new Service(clock);
    }
}
