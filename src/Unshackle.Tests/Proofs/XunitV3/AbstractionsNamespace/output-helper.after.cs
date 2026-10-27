using Xunit;

public class CalculatorTests
{
    private readonly ITestOutputHelper _output;

    public CalculatorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Add_Works()
    {
        _output.WriteLine("adding");
        Assert.Equal(4, 2 + 2);
    }
}
