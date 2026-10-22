// Expect: Threw System.InvalidOperationException
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
        service.Setup(s => s.Send(It.IsAny<int>(), It.IsAny<string>())).Throws(new InvalidOperationException());
        service.Object.Send(1, "a");
        return "sent";
    }
}
