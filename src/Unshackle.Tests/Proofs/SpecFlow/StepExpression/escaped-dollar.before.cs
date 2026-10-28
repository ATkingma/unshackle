using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class PriceSteps
{
    [Then(@"the price is 5\$")]
    public void ThenThePriceIsFive()
    {
    }
}
