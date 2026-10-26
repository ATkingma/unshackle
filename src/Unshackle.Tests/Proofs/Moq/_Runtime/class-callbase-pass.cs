using Moq;

namespace Proofs;

public class Account
{
    public static int Hits;

    public Account(int id, string owner)
    {
        Id = id;
        Owner = owner;
        Boot = Init();
        Hits++;
    }

    public int Id { get; }

    public string Owner { get; }

    public string Boot { get; }

    public virtual string Init() => "init";

    public virtual string Describe() => Owner + "#" + Id;

    public string Wrapped() => "[" + Describe() + "]";

    public virtual int Balance { get; set; } = 7;

    protected virtual string Secret() => "secret";

    public string Reveal() => Secret();

    public override string ToString() => "account " + Id;
}

public static class Probe
{
    public static string Run()
    {
        var account = new Mock<Account>(MockBehavior.Loose, 4, "ann") { CallBase = true };
        var other = new Mock<Account>(9, "bob") { CallBase = true }.Object;
        Account sut = account.Object;
        sut.Balance = 11;
        return string.Join("|", sut.Describe(), sut.Wrapped(), sut.Boot, sut.Balance, sut.Reveal(), sut.ToString(), other.Describe(), Account.Hits);
    }
}
