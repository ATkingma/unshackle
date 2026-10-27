using Xunit;

[assembly: CollectionBehavior(typeof(Tests.Ordering.PerClassFactory))]
[assembly: TestCollectionOrderer(typeof(Tests.Ordering.ByName))]

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

    [TestCaseOrderer(typeof(ByPriority))]
    public class OrderedTests
    {
        [Fact]
        public void First()
        {
        }
    }
}
