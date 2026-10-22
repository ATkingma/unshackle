// Expect: VerifyFailed
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
        var settings = new Mock<ISettings>();
        settings.SetupAllProperties();
        settings.Object.Level = 5;
        settings.VerifySet(s => s.Level = 3, Times.Once());
        return "verified";
    }
}
