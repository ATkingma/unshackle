// Expect: Threw System.ArgumentException
using Moq;

namespace Proofs;

public class Account
{
    public Account(int id)
    {
        Id = id;
    }

    public int Id { get; }
}

public static class Probe
{
    public static string Run()
    {
        // No constructor takes a string: both libraries refuse at run time.
        return new Mock<Account>("x") { CallBase = true }.Object.Id.ToString();
    }
}
