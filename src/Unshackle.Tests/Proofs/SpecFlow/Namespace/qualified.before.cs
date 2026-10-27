namespace Shop.Specs;

[TechTalk.SpecFlow.Binding]
public class QualifiedSteps
{
    [TechTalk.SpecFlow.Given("a step")]
    public void GivenAStep(TechTalk.SpecFlow.Table table, global::TechTalk.SpecFlow.ScenarioContext context)
    {
        context["rows"] = table.RowCount;
    }
}
