using Reqnroll;
using Reqnroll.Assist;

namespace Shop.Specs;

public class Product
{
    public string Name { get; set; } = "";

    public decimal Price { get; set; }
}

[Binding]
public class ProductSteps
{
    [Given("the product")]
    public void GivenTheProduct(Table table)
    {
        Product product = table.CreateInstance<Product>();
        table.CompareToInstance(product);
    }
}
