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
        var settings = Substitute.For<ISettings>();
        settings.Name.Returns("init");
        var sut = settings;
    }
}
