using System.Collections.Generic;
using Moq;
using NSubstitute;

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
        repo.Object.Count(Arg.Any<string>()).Returns(1).AndDoes(ci => _seen.Add(ci.ArgAt<string>(0)));
        var sut = repo.Object;
    }

    private void Record(string name) => _seen.Add(name);
}
