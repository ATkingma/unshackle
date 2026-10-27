using System;
using Reqnroll;

namespace Shop.Specs;

[Binding]
public class BasketSteps
{
    private readonly ScenarioContext _scenarioContext;

    public BasketSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"the basket contains (.*) items")]
    public void GivenTheBasketContains(int count) => _scenarioContext["count"] = count;

    [When("I add the following items")]
    public void WhenIAdd(Table table) => _scenarioContext["count"] = (int)_scenarioContext["count"] + table.RowCount;

    [Then(@"the basket has (\d+) items")]
    public void ThenTheBasketHas(int expected)
    {
        if ((int)_scenarioContext["count"] != expected)
        {
            throw new InvalidOperationException("count");
        }
    }

    [BeforeScenario("basket")]
    public void Reset() => _scenarioContext["count"] = 0;
}
