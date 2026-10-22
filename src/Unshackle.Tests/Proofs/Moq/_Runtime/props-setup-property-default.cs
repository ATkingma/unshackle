using Moq;

namespace Proofs;

public interface ISettings
{
    string Name { get; set; }
    int Level { get; set; }
    System.DateTime Created { get; }
}

public interface INamed
{
    string Name { get; set; }
}

public static class Probe
{
    public static string Run()
    {
        var named = new Mock<INamed>();
        named.SetupProperty(n => n.Name);
        INamed sut = named.Object;
        string first = sut.Name ?? "null";
        sut.Name = "b";
        return first + "|" + sut.Name;
    }
}
