using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

public class JsonDataAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod) => [[1]];
}

public class LogTestAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest) => Console.WriteLine(methodUnderTest.Name);
}

public class ByNameCollections : ITestCollectionOrderer
{
    public IEnumerable<Xunit.Abstractions.ITestCollection> OrderTestCollections(IEnumerable<Xunit.Abstractions.ITestCollection> collections) =>
        collections.OrderBy(c => c.DisplayName);
}
