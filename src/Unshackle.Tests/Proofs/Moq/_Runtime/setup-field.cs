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

public class Fixture
{
    private readonly Mock<IRepo> _repo;

    public Fixture()
    {
        _repo = new Mock<IRepo>();
        _repo.Setup(r => r.Find(2, "abc")).Returns("two");
        this._repo.Setup(r => r.TotalAsync(It.IsAny<int>())).ReturnsAsync(9);
    }

    public string Read() => Proofs.Read.Repo(_repo.Object);
}

public static class Probe
{
    public static string Run() => new Fixture().Read();
}
