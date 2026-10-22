using Moq;

namespace Proofs;

public interface IRepo
{
    System.DateTime Now { get; }
    int Count(string name);
    string Find(int id, string name);
    System.Threading.Tasks.Task<int> TotalAsync(int year);
}

public static class Read
{
    public static string Repo(IRepo r) =>
        $"{r.Now}|{r.Count("a")}|{r.Count("zz")}|{r.Find(1, "abc") ?? "null"}|{r.Find(1, "x") ?? "null"}|{r.Find(2, "abc") ?? "null"}|{r.TotalAsync(2020).Result}|{r.TotalAsync(1999).Result}";
}

public static class Probe
{
    public static string Run()
    {
        var repo = new Mock<IRepo>();
        repo.Setup(r => r.Count(It.IsAny<string>())).Returns(3);
        repo.Setup(r => r.Count("a")).Returns(7);
        repo.Setup(r => r.Find(It.IsAny<int>(), It.IsAny<string>())).Returns("any");
        repo.Setup(r => r.Find(1, "x")).Returns(null as string);
        return Read.Repo(repo.Object);
    }
}
