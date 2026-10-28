using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class LocationSteps
{
    // With the CucumberExpressions.SpecFlow plugin SpecFlow already matches "Lucy is at 0, 0" (corpus shouty.net); a regex pin breaks it.
    [Given("Lucy is at {int}, {int}")]
    public void GivenLucyIsAt(int x, int y)
    {
    }
}
