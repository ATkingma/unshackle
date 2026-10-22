using Moq;

namespace Proofs;

public interface IHolder
{
    int Value { get; set; }
}

public class CreateProof
{
    public void Test()
    {
        var holder = new Mock<IHolder>();
        var sut = holder.Object.Value;
    }
}
