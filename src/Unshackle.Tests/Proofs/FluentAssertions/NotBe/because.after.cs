using Shouldly;

namespace Proofs;

public enum Status
{
    Placed,
    Shipped,
}

public class Because
{
    public void Test(Status status)
    {
        status.ShouldNotBe(Status.Shipped, "a new order is not shipped yet");
    }
}
