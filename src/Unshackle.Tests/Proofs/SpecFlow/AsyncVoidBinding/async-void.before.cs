using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class AsyncSteps
{
    [When("the order is sent")]
    public async void WhenTheOrderIsSent()
    {
        await Task.Delay(1);
    }

    [AfterScenario]
    public async void Cleanup()
    {
        await Task.Yield();
    }
}
