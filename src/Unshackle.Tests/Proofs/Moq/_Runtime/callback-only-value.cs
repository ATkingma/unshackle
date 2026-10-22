using System;
using System.Collections.Generic;
using Moq;

namespace Proofs;

public interface IRepo
{
    int Count(string name);
    void Save(int id, string text);
    int Total { get; }
}

public static class Probe
{
    public static string Run()
    {
        int calls = 0;
        var repo = new Mock<IRepo>();
        repo.Setup(r => r.Count(It.IsAny<string>())).Callback(() => calls++);
        int value = repo.Object.Count("x");
        return value + "|" + calls;
    }
}
