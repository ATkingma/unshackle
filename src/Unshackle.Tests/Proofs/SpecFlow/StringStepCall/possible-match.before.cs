using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class LoginSteps : Steps
{
    [Given("I am logged in")]
    public void GivenIAmLoggedIn()
    {
        Given("I open the login page");
    }

    [Given(@"I open the (.*) page")]
    public void GivenIOpenThePage(string page)
    {
    }
}
