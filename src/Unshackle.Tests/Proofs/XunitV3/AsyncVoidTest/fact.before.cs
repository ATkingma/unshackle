using System.Threading.Tasks;
using Xunit;

public class ServiceTests
{
    [Fact]
    public async void Call_Completes()
    {
        await Task.Yield();
        Assert.True(true);
    }

    [Theory]
    [InlineData(1)]
    public async void Value_IsPositive(int value)
    {
        await Task.Delay(1);
        Assert.True(value > 0);
    }

    public async void NotATest()
    {
        await Task.Yield();
    }
}
