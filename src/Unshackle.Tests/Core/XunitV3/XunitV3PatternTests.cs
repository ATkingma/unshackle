using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.XunitV3;
using Unshackle.Core.Fixes.XunitV3.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.XunitV3;

public class XunitV3PatternTests
{
    [Fact]
    public void AsyncLifetime_Find_OwnInitializeAsyncWithoutTheInterface_NotFound()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("using System.Threading.Tasks; public class C { public Task InitializeAsync() => Task.CompletedTask; }");

        // Act
        int found = new AsyncLifetimePattern().Find(root, model).Count();

        // Assert
        found.ShouldBe(0);
    }

    [Fact]
    public void AsyncLifetime_Find_OverrideOfAnImplementation_Found()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("using System.Threading.Tasks; using Xunit;"
            + " public class B : IAsyncLifetime { public virtual Task InitializeAsync() => Task.CompletedTask; public Task DisposeAsync() => Task.CompletedTask; }"
            + " public class D : B { public override async Task InitializeAsync() { await base.InitializeAsync(); } }");
        var pattern = new AsyncLifetimePattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldAllBe(r => r is FixedResult);
    }

    [Fact]
    public void AsyncVoidTest_Find_CustomFactAttribute_Found()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("using System.Threading.Tasks; using Xunit;"
            + " public class SlowFactAttribute : FactAttribute { }"
            + " public class T { [SlowFact] public async void A() { await Task.Yield(); } }");

        // Act
        int found = new AsyncVoidTestPattern().Find(root, model).Count();

        // Assert
        found.ShouldBe(1);
    }

    [Fact]
    public async Task AbstractionsNamespace_GlobalUsingWithSinkInAnotherFile_Manual()
    {
        // Arrange: the global using serves every file, so another file's IMessageSink blocks the swap.
        Solution solution = TestSolution.Create("global using Xunit.Abstractions;", XunitV3References.Before);
        Project project = solution.Projects.Single();
        project = project.AddDocument("Other.cs", "public class S { public S(IMessageSink sink) { } }", filePath: "Other.cs").Project;
        Document document = project.Documents.First(d => d.Name != "Other.cs");
        SyntaxNode root = (await document.GetSyntaxRootAsync(TestContext.Current.CancellationToken))!;
        SemanticModel model = (await document.GetSemanticModelAsync(TestContext.Current.CancellationToken))!;
        var pattern = new AbstractionsNamespacePattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(XunitV3Reason.ExtensibilityType));
    }

    [Fact]
    public void AbstractionsNamespace_GlobalUsing_BecomesGlobalUsingXunit()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("global using Xunit.Abstractions;");
        var pattern = new AbstractionsNamespacePattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.ShouldHaveSingleItem().ToString().ShouldBe("global using Xunit;");
    }

    [Fact]
    public void RecordExceptionAsync_Find_OwnRecordClassOrParenthesizedAwait_NotFound()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("using System; using System.Threading.Tasks;"
            + " public static class Record { public static Task<Exception> ExceptionAsync(Func<Task> f) => Task.FromResult<Exception>(null); }"
            + " public class T { public async Task A() { var e = Record.ExceptionAsync(() => Task.CompletedTask); var x = await (Xunit.Record.ExceptionAsync(() => Task.CompletedTask)); } }");

        // Act
        int found = new RecordExceptionAsyncPattern().Find(root, model).Count();

        // Assert
        found.ShouldBe(0);
    }

    [Theory]
    [InlineData("[assembly: TestCaseOrderer(\"O\", \"Other\")] public class O { }", "not a type of this project")]
    [InlineData("[assembly: TestCaseOrderer(\"Missing\", \"Test\")]", "not a type of this project")]
    public void TypeNameAttribute_NotOwnConstantType_Manual(string code, string why)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("using Xunit; " + code);
        var pattern = new TypeNameAttributePattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        ManualResult manual = result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe(nameof(XunitV3Reason.TypeNameArgument));
        manual.Reason.ShouldContain(why);
        manual.Reason.ShouldContain("no v2 → v3 name map");
        manual.Reason.ShouldNotContain("Hint:");
    }

    [Fact]
    public void TypeNameAttribute_EnumConstructor_NotFound()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("using Xunit; [assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass)]");

        // Act
        int found = new TypeNameAttributePattern().Find(root, model).Count();

        // Assert
        found.ShouldBe(0);
    }

    [Fact]
    public void Extensibility_Find_FactSubclassNotFoundDiscovererAttributeFound()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("using Xunit; using Xunit.Sdk;"
            + " public class SlowFactAttribute : FactAttribute { }"
            + " [XunitTestCaseDiscoverer(\"D\", \"Test\")] public class RetryFactAttribute : FactAttribute { }"
            + " public class Data : TheoryData<int> { }");
        var pattern = new ExtensibilityPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldHaveSingleItem().ShouldBeOfType<ManualResult>().Reason.ShouldContain("RetryFactAttribute builds on the v2 extensibility API (Xunit.Sdk.XunitTestCaseDiscovererAttribute)");
    }

    [Fact]
    public async Task SdkUse_ChangedMembersProof_DoesNotBuildOnV3()
    {
        // Arrange: council 22 X12; TestTimeoutException keeps its name in v3 but lost the int constructor, RunSummary is gone.
        string code = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Proofs", "XunitV3", "SdkUse", "changed-members.before.cs"),
            TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<Diagnostic> errors = TestSolution.CompileErrors(code, XunitV3References.After);

        // Assert
        TestSolution.CompileErrors(code, XunitV3References.Before).ShouldBeEmpty();
        errors.ShouldContain(e => e.GetMessage(null).Contains("TestTimeoutException", StringComparison.Ordinal));
        errors.ShouldContain(e => e.GetMessage(null).Contains("RunSummary", StringComparison.Ordinal));
    }

    [Fact]
    public void SdkUse_Find_UsingAndDeclarationBaseOnly_NotFound()
    {
        // Arrange: the base list is ExtensibilityPattern's Manual row; a using is not a use.
        (SyntaxNode root, SemanticModel model) = Parse("using System.Collections.Generic; using System.Reflection; using Xunit.Sdk;"
            + " public class D : DataAttribute { public override IEnumerable<object[]> GetData(MethodInfo m) => [[1]]; }");

        // Act
        int found = new SdkUsePattern().Find(root, model).Count();

        // Assert
        found.ShouldBe(0);
    }

    [Fact]
    public void SdkUse_GenericAndQualifiedUses_OneInfoRow()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("public class C {\n Xunit.Sdk.ExceptionAggregator a;\n Xunit.Sdk.ExceptionAggregator b;\n Xunit.Sdk.DelegatingMessageBus<Xunit.Abstractions.IMessageSinkMessage> c; }");
        var pattern = new SdkUsePattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldHaveSingleItem().ShouldBeOfType<InfoResult>().Text.ShouldContain(
            "ExceptionAggregator (lines 2, 3; no same-name type in xunit.v3 3.2.2), DelegatingMessageBus (line 4; no same-name type in xunit.v3 3.2.2)");
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("P", [tree], XunitV3References.Before, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return (tree.GetRoot(), compilation.GetSemanticModel(tree));
    }
}
