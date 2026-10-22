using System;
using Moq;
using NSubstitute;

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
        service.Object.When(s => s.Send(Arg.Is<int>(1), Arg.Any<string>())).Do(_ => throw new InvalidOperationException());
        var sut = service.Object;
    }

    private static Exception Make() => new InvalidOperationException();
}
