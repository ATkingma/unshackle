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
        settings.Setup(s => s.Created).Returns(System.DateTime.MinValue);
        settings.SetupAllProperties();
        var sut = settings.Object;
    }
}
