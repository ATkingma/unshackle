using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class PaySteps
{
    private const string Pay = "I pay with card/cash";

    [When(Pay)]
    public void WhenIPay()
    {
    }
}
