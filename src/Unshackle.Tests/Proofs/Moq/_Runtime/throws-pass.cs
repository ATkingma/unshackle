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
        var service = new Mock<IService>();
        var shared = new ArgumentException("shared");
        service.Setup(s => s.Count("a")).Throws(new InvalidOperationException("boom"));
        service.Setup(s => s.Count("b")).Throws<NotSupportedException>();
        service.Setup(s => s.Count("c")).Throws(shared);
        service.Setup(s => s.Send(1, It.IsAny<string>())).Throws(new InvalidOperationException("void"));
        service.Setup(s => s.Send(2, "x")).Throws(shared);
        service.Setup(s => s.TotalAsync(1)).ThrowsAsync(new TimeoutException("late"));
        IService sut = service.Object;
        var total = sut.TotalAsync(1);
        return string.Join("|",
            Try.Run(() => sut.Count("a")), Try.Run(() => sut.Count("b")), Try.Run(() => sut.Count("c")), Try.Run(() => sut.Count("z")),
            Try.Run(() => sut.Send(1, "q")), Try.Run(() => sut.Send(2, "x")), Try.Run(() => sut.Send(3, "x")),
            total.IsFaulted + ":" + total.Exception?.InnerException?.Message, Try.Run(() => sut.TotalAsync(2).Wait()));
    }
}
