using System.Threading.Tasks;
using Xunit;

public class ServiceTests
{
    [Fact]
    public async void Call_Completes()
    {
        await Task.Yield();
    }

    public void Again() => Call_Completes();
}
