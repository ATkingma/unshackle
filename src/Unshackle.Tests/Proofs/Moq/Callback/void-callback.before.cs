using System.Collections.Generic;
using Moq;

namespace Proofs;

public interface IRepo
{
    int Count(string name);
    void Save(int id, string text);
    int Total { get; }
}

public class CallbackProof
{
    private readonly List<string> _seen = new List<string>();
    private int _calls;

    public void Test()
    {
        var repo = new Mock<IRepo>();
        repo.Setup(r => r.Save(It.IsAny<int>(), It.IsAny<string>())).Callback((int id, string text) => _seen.Add(text + id));
        var sut = repo.Object;
    }

    private void Record(string name) => _seen.Add(name);
}
