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
        repo.Object.When(r => r.Save(Arg.Any<int>(), Arg.Any<string>())).Do(ci => _seen.Add(ci.ArgAt<string>(1) + ci.ArgAt<int>(0)));
        var sut = repo.Object;
    }

    private void Record(string name) => _seen.Add(name);
}
