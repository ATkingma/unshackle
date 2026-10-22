// Expect: Threw System.InvalidOperationException
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
        var repo = new Mock<IRepo>();
        repo.Setup(r => r.Save(It.IsAny<int>(), It.IsAny<string>())).Callback(() => throw new InvalidOperationException());
        repo.Object.Save(1, "a");
        return "saved";
    }
}
