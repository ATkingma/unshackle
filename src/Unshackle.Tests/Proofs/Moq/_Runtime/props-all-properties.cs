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
        settings.SetupGet(s => s.Created).Returns(new System.DateTime(2020, 1, 1));
        ISettings sut = settings.Object;
        string before = (sut.Name is null) + "/" + sut.Level;
        sut.Level = 5;
        sut.Name = "x";
        string after = sut.Name + "/" + sut.Level + "/" + sut.Created.Year;
        settings.VerifySet(s => s.Level = 5, Times.Once());
        settings.VerifySet(s => s.Name = It.IsAny<string>(), Times.Once());
        settings.VerifyGet(s => s.Created, Times.Once());
        return before + "|" + after;
    }
}
