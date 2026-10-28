using Reqnroll;

namespace Shop.Specs;

[Binding]
public class PaySteps
{
    private const string Pay = "I pay with card/cash";

    [When(Pay, ExpressionType = ExpressionType.RegularExpression)]
    public void WhenIPay()
    {
    }
}
