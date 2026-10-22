// Expect: Threw System.NullReferenceException
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
        repo.Setup(r => r.Count(It.IsAny<string>())).Returns((string n) => n.Length);
        return repo.Object.Count(null).ToString();
    }
}
