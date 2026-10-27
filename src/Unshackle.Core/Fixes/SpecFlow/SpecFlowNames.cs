using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.SpecFlow;

// The name map SpecFlow 3.9.74 → Reqnroll 3.3.4. Reqnroll is a fork of SpecFlow v4: every public type keeps its name under the swapped
// namespace (TechTalk.SpecFlow[.X] → Reqnroll[.X], BoDi → Reqnroll.BoDi), except the ones below (checked against both real
// assemblies by SpecFlowNamesTests).
internal static class SpecFlowNames
{
    public const string OldRoot = "TechTalk.SpecFlow";

    public const string NewRoot = "Reqnroll";

    public const string OldContainer = "BoDi";

    public const string NewContainer = "Reqnroll.BoDi";

    // Council round 18, S8: the Autofac plugin, the one SpecFlow plugin with a Reqnroll port (SpecFlow.Autofac → Reqnroll.Autofac).
    public const string OldAutofac = "SpecFlow.Autofac";

    public const string NewAutofac = "Reqnroll.Autofac";

    // Types renamed (SpecFlow → Reqnroll in the name, TableRow → DataTableRow) or moved to another namespace, as full metadata names.
    public static IReadOnlyDictionary<string, string> RenamedTypes { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["TechTalk.SpecFlow.ISpecFlowContext"] = "Reqnroll.IReqnrollContext",
        ["TechTalk.SpecFlow.SpecFlowContext"] = "Reqnroll.ReqnrollContext",
        ["TechTalk.SpecFlow.SpecFlowException"] = "Reqnroll.ReqnrollException",
        ["TechTalk.SpecFlow.TableRow"] = "Reqnroll.DataTableRow",
        ["TechTalk.SpecFlow.TableRows"] = "Reqnroll.DataTableRows",
        ["TechTalk.SpecFlow.Infrastructure.ISpecFlowOutputHelper"] = "Reqnroll.IReqnrollOutputHelper",
        ["TechTalk.SpecFlow.Infrastructure.SpecFlowOutputHelper"] = "Reqnroll.Infrastructure.ReqnrollOutputHelper",
        ["TechTalk.SpecFlow.Infrastructure.ISpecFlowAttachmentHandler"] = "Reqnroll.Infrastructure.IReqnrollAttachmentHandler",
        ["TechTalk.SpecFlow.Infrastructure.SpecFlowAttachmentHandler"] = "Reqnroll.Infrastructure.ReqnrollAttachmentHandler",
        ["TechTalk.SpecFlow.Configuration.SpecFlowConfiguration"] = "Reqnroll.Configuration.ReqnrollConfiguration",
        ["TechTalk.SpecFlow.Configuration.ISpecFlowConfigurationHolder"] = "Reqnroll.Configuration.IReqnrollConfigurationHolder",
        ["TechTalk.SpecFlow.Configuration.ISpecFlowJsonLocator"] = "Reqnroll.Configuration.IReqnrollJsonLocator",
        ["TechTalk.SpecFlow.Configuration.SpecFlowJsonLocator"] = "Reqnroll.Configuration.ReqnrollJsonLocator",
        ["TechTalk.SpecFlow.Plugins.ISpecFlowPath"] = "Reqnroll.Plugins.IReqnrollPath",
        ["TechTalk.SpecFlow.Plugins.SpecFlowPath"] = "Reqnroll.Plugins.ReqnrollPath",
        ["TechTalk.SpecFlow.Parser.SpecFlowGherkinDialectProvider"] = "Reqnroll.Parser.ReqnrollGherkinDialectProvider",
        ["TechTalk.SpecFlow.Bindings.Discovery.ISpecFlowAttributesFilter"] = "Reqnroll.Bindings.Discovery.IReqnrollAttributesFilter",
        ["TechTalk.SpecFlow.Bindings.Discovery.SpecFlowAttributesFilter"] = "Reqnroll.Bindings.Discovery.ReqnrollAttributesFilter",
        ["TechTalk.SpecFlow.Analytics.EnvironmentSpecFlowTelemetryChecker"] = "Reqnroll.Analytics.EnvironmentReqnrollTelemetryChecker",
        ["TechTalk.SpecFlow.Analytics.IEnvironmentSpecFlowTelemetryChecker"] = "Reqnroll.Analytics.IEnvironmentReqnrollTelemetryChecker",
        ["TechTalk.SpecFlow.Analytics.SpecFlowAnalyticsEventBase"] = "Reqnroll.Analytics.ReqnrollAnalyticsEventBase",
        ["TechTalk.SpecFlow.Analytics.SpecFlowProjectCompilingEvent"] = "Reqnroll.Analytics.ReqnrollProjectCompilingEvent",
        ["TechTalk.SpecFlow.Analytics.SpecFlowProjectRunningEvent"] = "Reqnroll.Analytics.ReqnrollProjectRunningEvent",
        // The Assist extension methods (CreateInstance, CreateSet, CompareToInstance, ...) live in namespace Reqnroll.
        ["TechTalk.SpecFlow.Assist.ComparisonException"] = "Reqnroll.ComparisonException",
        ["TechTalk.SpecFlow.Assist.InstanceComparisonExtensionMethods"] = "Reqnroll.InstanceComparisonExtensionMethods",
        ["TechTalk.SpecFlow.Assist.SetComparisonExtensionMethods"] = "Reqnroll.SetComparisonExtensionMethods",
        ["TechTalk.SpecFlow.Assist.TableHelperExtensionMethods"] = "Reqnroll.TableHelperExtensionMethods",
        ["TechTalk.SpecFlow.Assist.TableHelpers"] = "Reqnroll.TableHelpers",
        ["TechTalk.SpecFlow.TestFramework.DefaultTestRunContext"] = "Reqnroll.DefaultTestRunContext",
        ["TechTalk.SpecFlow.TestFramework.ITestRunContext"] = "Reqnroll.ITestRunContext",
    };

    // Namespaces without a Reqnroll twin: App.config configuration only works through Reqnroll.SpecFlowCompatibility.
    public static IReadOnlyList<string> RemovedNamespaces { get; } = ["TechTalk.SpecFlow.Configuration.AppConfig"];

    // Types without a Reqnroll twin (plumbing of the synchronous binding invoker, App.config, the plugin resolver).
    public static IReadOnlyList<string> RemovedTypes { get; } =
    [
        "TechTalk.SpecFlow.Bindings.AsyncHelpers",
        "TechTalk.SpecFlow.Bindings.IBindingInvoker",
        "TechTalk.SpecFlow.Bindings.ISynchronousBindingDelegateInvoker",
        "TechTalk.SpecFlow.Bindings.SynchronousBindingDelegateInvoker",
        "TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler",
        "TechTalk.SpecFlow.Plugins.PluginAssemblyResolver",
        "BoDi.BoDiConfigurationSection",
        "BoDi.ContainerRegistrationCollection",
        "BoDi.ContainerRegistrationConfigElement",
    ];

    // The Reqnroll namespace for a SpecFlow or BoDi namespace; null when it is not one.
    public static string? NewNamespace(string oldNamespace) =>
        oldNamespace == OldRoot ? NewRoot
        : oldNamespace.StartsWith(OldRoot + ".", StringComparison.Ordinal) ? NewRoot + oldNamespace[OldRoot.Length..]
        : oldNamespace == OldContainer ? NewContainer
        : oldNamespace == OldAutofac ? NewAutofac
        : oldNamespace.StartsWith(OldAutofac + ".", StringComparison.Ordinal) ? NewAutofac + oldNamespace[OldAutofac.Length..]
        : null;

    public static bool IsOldNamespace(string name) => NewNamespace(name) is not null;

    public static bool IsRemovedNamespace(string name) =>
        RemovedNamespaces.Any(ns => name == ns || name.StartsWith(ns + ".", StringComparison.Ordinal));

    // Full metadata name of a type, for example TechTalk.SpecFlow.Assist.TableHelperExtensionMethods.
    public static string FullName(INamedTypeSymbol type) =>
        type.ContainingType is INamedTypeSymbol outer ? FullName(outer) + "+" + type.MetadataName
        : type.ContainingNamespace.IsGlobalNamespace ? type.MetadataName
        : type.ContainingNamespace.ToDisplayString() + "." + type.MetadataName;

    // A type of SpecFlow's assemblies (TechTalk.SpecFlow or BoDi).
    public static bool IsOldType(ISymbol? symbol) =>
        symbol is INamedTypeSymbol named && IsOldNamespace(named.OriginalDefinition.ContainingNamespace.ToDisplayString());

    public static string NamespaceOf(string fullName) => fullName[..fullName.LastIndexOf('.')];

    public static string SimpleNameOf(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];
}
