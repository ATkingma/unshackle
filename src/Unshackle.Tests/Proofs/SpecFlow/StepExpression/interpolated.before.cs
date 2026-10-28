using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class PaySteps
{
    private const string Means = "card/cash";

    [When($"I pay with {Means}")]
    public void WhenIPay()
    {
    }
}
