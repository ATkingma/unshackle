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
        named.SetupProperty(n => n.Name, "init");
        INamed sut = named.Object;
        string first = sut.Name;
        sut.Name = "b";
        named.VerifySet(n => n.Name = "b", Times.Once());
        return first + "|" + sut.Name;
    }
}
