using TechTalk.SpecFlow;

namespace Shop.Specs;

[Binding]
public class LoginSteps : Steps
{
    [Given("I am logged in")]
    public void GivenIAmLoggedIn()
    {
        Given("I open the login page");
        When("I enter valid credentials");
    }
}
