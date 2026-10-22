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
        var log = new List<string>();
        var repo = new Mock<IRepo>();
        repo.Setup(r => r.Count(It.IsAny<string>())).Returns(3).Callback((string n) => log.Add("C" + n));
        repo.Setup(r => r.Total).Returns(9);
        repo.Setup(r => r.Save(It.Is<int>(i => i > 0), It.IsAny<string>())).Callback((int id, string text) => log.Add(text + id));
        IRepo sut = repo.Object;
        int a = sut.Count("abc");
        int b = sut.Total;
        sut.Save(2, "s");
        sut.Save(0, "skip");
        return a + "|" + b + "|" + string.Join(",", log);
    }
}
