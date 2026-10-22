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
        settings.Object.Level = 2;
        settings.VerifySet(s => s.Level = It.IsAny<int>(), Times.Once());
        settings.VerifySet(s => s.Name = "a", Times.Never());
    }
}
