using System;
using System.Linq;
using Moq;

namespace Proofs;

public interface IFeed
{
    string Label { get; }
    int Next(string key);
    System.Threading.Tasks.Task<int> NextAsync();
}

public static class Probe
{
    // Only the values inside the sequence: after the last one Moq answers its default and NSubstitute repeats the last value.
    public static string Run()
    {
        var feed = new Mock<IFeed>();
        feed.SetupSequence(f => f.Next("a")).Returns(1).Returns(2).Returns(3);
        feed.SetupSequence(f => f.Label).Returns("x").Returns("y");
        feed.SetupSequence(f => f.NextAsync()).ReturnsAsync(7).ReturnsAsync(8);
        IFeed sut = feed.Object;
        return string.Join("|", sut.Next("a"), sut.Next("a"), sut.Next("a"), sut.Label, sut.Label,
            sut.NextAsync().Result, sut.NextAsync().Result);
    }
}
