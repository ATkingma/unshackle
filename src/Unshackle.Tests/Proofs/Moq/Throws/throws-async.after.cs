using System;
using Moq;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
        service.Object.TotalAsync(1).ThrowsAsync(new TimeoutException());
        var sut = service.Object;
    }

    private static Exception Make() => new InvalidOperationException();
}
