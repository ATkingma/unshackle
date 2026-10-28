using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class VersionSteps
{
    // The CucumberExpressions.SpecFlow plugin reads this as a Cucumber expression, Reqnroll 3.3.4 as a regex (\d+).
    [Given(@"version \d+ is installed")]
    public void GivenVersionInstalled()
    {
    }
}
