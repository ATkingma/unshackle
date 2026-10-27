using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class PendingSteps
{
    private readonly ScenarioContext _scenarioContext;

    public PendingSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("something not built yet")]
    public void GivenSomethingNotBuiltYet()
    {
        _scenarioContext.Pending();
    }

    [When("it is used")]
    public void WhenItIsUsed() => _scenarioContext.Pending();
}
