using Reqnroll;

namespace Shop.Specs;

[Binding]
public class ListSteps
{
    [Then("the item(s) are listed", ExpressionType = ExpressionType.RegularExpression)]
    public void ThenTheItemsAreListed()
    {
    }
}
