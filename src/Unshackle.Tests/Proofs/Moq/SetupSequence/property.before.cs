using Moq;

namespace Proofs;

public interface IRepo
{
    string Label { get; }
    int Count(string name);
    void Send(int id);
    System.Threading.Tasks.Task<int> TotalAsync(int year);
}

public class SequenceProof
{
    public void Test()
    {
        var repo = new Mock<IRepo>();
        repo.SetupSequence(r => r.Label).Returns("a").Returns("b");
        var sut = repo.Object;
    }
}
