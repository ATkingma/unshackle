using Moq;
using NSubstitute;

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
        _ = settings.Object.Created;
        _ = settings.Object.Received(1).Created;
    }
}
