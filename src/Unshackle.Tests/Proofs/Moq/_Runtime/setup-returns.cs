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
        repo.Setup(r => r.Now).Returns(new System.DateTime(2020, 1, 2));
        repo.Setup(r => r.Count(It.IsAny<string>())).Returns(3);
        repo.Setup(r => r.Find(1, It.Is<string>(n => n.Length > 2))).Returns("found");
        repo.Setup(r => r.TotalAsync(2020)).ReturnsAsync(5);
        return Read.Repo(repo.Object);
    }
}
