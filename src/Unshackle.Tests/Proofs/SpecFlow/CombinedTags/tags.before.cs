using System.Linq;
using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class TagHooks
{
    private readonly ScenarioContext _scenarioContext;

    public TagHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario]
    public void Check()
    {
        bool slow = _scenarioContext.ScenarioInfo.ScenarioAndFeatureTags.Contains("slow");
        _scenarioContext["slow"] = slow;
    }
}
