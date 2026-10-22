using Moq;

namespace Proofs;

public interface IRepo
{
    System.DateTime Now { get; }
    int Count(string name);
    string Find(int id, string name);
    System.Threading.Tasks.Task<int> TotalAsync(int year);
}

public static class Probe
{
    public static string Run()
    {
        var repo = new Mock<IRepo>();
        repo.Setup(r => r.Find(It.IsAny<int>(), It.IsAny<string>())).Returns((int id, string ci) =>
        {
            string text = ci.ToUpperInvariant();
            return text + id;
        });
        return repo.Object.Find(7, "ok");
    }
}
