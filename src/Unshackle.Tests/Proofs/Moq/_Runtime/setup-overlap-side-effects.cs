using System;
using Moq;

namespace Proofs;

public interface IService
{
    int Count(string name);
    void Send(int id, string text);
    System.Threading.Tasks.Task<int> TotalAsync(int year);
}

public static class Try
{
    public static string Run(Action action)
    {
        try
        {
            action();
            return "ok";
        }
        catch (Exception ex)
        {
            return ex.GetType().Name + ":" + ex.Message;
        }
    }
}

public static class Probe
{
    public static string Run()
    {
        int calls = 0;
        var service = new Mock<IService>();
        service.Setup(s => s.Count(It.IsAny<string>())).Throws(new InvalidOperationException("any"));
        service.Setup(s => s.Count("a")).Returns(1);
        service.Setup(s => s.TotalAsync(It.IsAny<int>())).Returns((int y) => System.Threading.Tasks.Task.FromResult(y + calls++));
        service.Setup(s => s.TotalAsync(5)).ReturnsAsync(50);
        IService sut = service.Object;
        return string.Join("|", Try.Run(() => sut.Count("a")), Try.Run(() => sut.Count("b")), sut.TotalAsync(5).Result, sut.TotalAsync(6).Result, calls);
    }
}
