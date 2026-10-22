using Moq;

namespace Proofs;

public interface INames
{
    string Name();
}

public class CreateProof
{
    public void Test()
    {
        var names = new Mock<INames>();
        var sut = names.Object.Name();
    }
}
