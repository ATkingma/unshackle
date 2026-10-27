namespace Shop.Specs;

[Reqnroll.Binding]
public class QualifiedSteps
{
    [Reqnroll.Given("a step")]
    public void GivenAStep(Reqnroll.Table table, global::Reqnroll.ScenarioContext context)
    {
        context["rows"] = table.RowCount;
    }
}
