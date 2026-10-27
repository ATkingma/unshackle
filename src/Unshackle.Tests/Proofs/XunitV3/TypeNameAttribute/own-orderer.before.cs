using Xunit;

[assembly: CollectionBehavior("Tests.Ordering.PerClassFactory", "Test")]
[assembly: TestCollectionOrderer("Tests.Ordering.ByName", "Test")]

namespace Tests.Ordering
{
    public class PerClassFactory
    {
    }

    public class ByName
    {
    }

    public class ByPriority
    {
    }

    [TestCaseOrderer("Tests.Ordering.ByPriority", "Test")]
    public class OrderedTests
    {
        [Fact]
        public void First()
        {
        }
    }
}
