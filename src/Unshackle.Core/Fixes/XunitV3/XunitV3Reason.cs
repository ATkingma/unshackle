namespace Unshackle.Core.Fixes.XunitV3;

// Typed reason codes of the xunit3 Manual rows (council round 10, C1).
public enum XunitV3Reason
{
    // A type of Xunit.Abstractions other than ITestOutputHelper: v3 redesigned the extensibility API (runners, discoverers, sinks).
    ExtensibilityType,

    // An own ITestOutputHelper implementation: the v3 interface has more members (Output, Write).
    OutputHelperImplemented,

    // IAsyncLifetime next to IDisposable or IAsyncDisposable: v3 IAsyncLifetime extends IAsyncDisposable and xUnit then skips Dispose.
    DualDisposal,

    // InitializeAsync/DisposeAsync used as a Task (not awaited directly): the ValueTask return type would not fit there.
    LifetimeResultUsed,

    // An async void test method that is also called or referenced by code.
    AsyncVoidCalled,

    // [TestCaseOrderer]/[CollectionBehavior]/[TestCollectionOrderer]/[TestFramework] with names that are not constant strings or name a type
    // outside the project: v3 only takes typeof(..).
    TypeNameArgument,

    // A shape the pattern does not rewrite (an aliased return type, for example).
    UnsupportedShape,
}
