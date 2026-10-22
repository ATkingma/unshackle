using System;
using Moq;

namespace Proofs;

public interface IService
{
    int Count(string name);
    void Send(int id, string text);
    System.Threading.Tasks.Task<int> TotalAsync(int year);
}

public class ThrowsProof
{
    public void Test()
    {
        var service = new Mock<IService>();
        service.Setup(s => s.Send(1, "a")).Throws(Make());
        var sut = service.Object;
    }

    private static Exception Make() => new InvalidOperationException();
}
