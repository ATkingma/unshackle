using System.Linq;
using Reqnroll;

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
        bool slow = _scenarioContext.ScenarioInfo.CombinedTags.Contains("slow");
        _scenarioContext["slow"] = slow;
    }
}
