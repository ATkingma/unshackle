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
        int calls = 0;
        var repo = new Mock<IRepo>();
        repo.Setup(r => r.Count(It.IsAny<string>())).Returns((string n) => n.Length + calls++);
        repo.Setup(r => r.Find(It.IsAny<int>(), It.Is<string>(s => s.Length > 1))).Returns((int id, string name) => name + id);
        repo.Setup(r => r.Now).Returns(() => new System.DateTime(2000 + calls, 1, 1));
        repo.Setup(r => r.TotalAsync(It.IsAny<int>())).ReturnsAsync((int year) => year * 2);
        IRepo sut = repo.Object;
        return string.Join("|", sut.Count("ab"), sut.Count("abc"), sut.Now.Year, sut.Find(3, "xy"), sut.Find(3, "x") ?? "null", sut.TotalAsync(21).Result);
    }
}
