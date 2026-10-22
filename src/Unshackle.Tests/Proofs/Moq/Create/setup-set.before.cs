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
        settings.SetupAllProperties();
        settings.SetupSet(s => s.Level = 1);
        var sut = settings.Object;
    }
}
