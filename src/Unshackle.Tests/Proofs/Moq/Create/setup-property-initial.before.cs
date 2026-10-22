using Moq;

namespace Proofs;

public interface ISettings
{
    string Name { get; set; }
    int Level { get; set; }
    System.DateTime Created { get; }
}

public class PropertiesProof
{
    public void Test()
    {
        var settings = new Mock<ISettings>();
        settings.SetupProperty(s => s.Name, "init");
        settings.SetupProperty(s => s.Level);
        var sut = settings.Object;
    }
}
