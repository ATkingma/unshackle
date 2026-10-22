using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Proofs;

public interface INames
{
    string Name();
}

public class CreateProof
{
    public void Test()
    {
        var names = Substitute.For<INames>();
        names.Name().ReturnsNull();
        var sut = names.Name();
    }
}
