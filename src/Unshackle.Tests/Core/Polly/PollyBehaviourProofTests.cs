using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

// Council round 8, general rule: a Fixed shape needs a proof that pins the behaviour that could change. Each Fixed proof in
// Proofs/Polly/<Pattern> runs before (Polly v7) and after (Polly.Core 8); Proof.RunAsync returns a trace of attempts,
// delay arguments and the outcome, and both traces must be equal.
public class PollyBehaviourProofTests
{
    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "Polly");

    [Theory]
    [InlineData("WaitAndRetryAsync/local-block-delay", "delay 1, delay 2, delay 3, caught call 4, calls 4")]
    [InlineData("WaitAndRetryAsync/inline-result", "call 1, call 2, call 3, result 30")]
    [InlineData("WaitAndRetryAsync/typed-local-not-handled", "caught call 1, calls 1")]
    [InlineData("WaitAndRetryAsync/filter-or-predicate", "throw t, throw go, throw stop, caught InvalidOperationException stop")]
    [InlineData("WaitAndRetryAsync/filter-handle-result", "got -1, got 0, got 5, result 5, got -2, got -3, got -4, result -4")]
    [InlineData("WaitAndRetryAsync/filter-policy-of-t", "result ok left 0")]
    [InlineData("WaitAndRetryAsync/on-retry-attempt", "delay 1, retry 1 after 1 call 1, delay 2, retry 2 after 2 call 2, caught call 3")]
    [InlineData("WaitAndRetryAsync/on-retry-async", "retry after call 1, retry after call 2, result 3")]
    [InlineData("WaitAndRetryAsync/field-static", "call 1, call 2, fail, fail, fail, caught always")]
    [InlineData("WaitAndRetryAsync/field-constructor", "retry after 1, retry after 2, calls 3")]
    [InlineData("RetryVariants/field-instance", "first 3, second -6, left 0")]
    [InlineData("AddPolicyHandler/inline-transient", "send 500, send 0, send 408, send 200, final 200")]
    [InlineData("AddPolicyHandler/policy-method", "send 502, send 504, final 504")]
    [InlineData("AddPolicyHandler/transient-lambda-429", "send 503, retry 1 503, send 429, final 429")]
    [InlineData("AddPolicyHandler/transient-or-status", "send 503, retry 1 503, send 429, final 429")]
    [InlineData("Wrap/two-locals", "inner 1, inner 2, outer 1, inner 1, done")]
    [InlineData("Wrap/three-inline", "argument 1, invalid 1, timeout 1, argument 1, invalid 1, done")]
    [InlineData("RetryVariants/retry-count", "retry 1 call 1, retry 2 call 2, caught call 3")]
    [InlineData("RetryVariants/retry-default", "call 1, call 2, caught call 2")]
    [InlineData("RetryVariants/wait-forever", "delay 1, delay 2, delay 3, delay 4, result 5 calls 9")]
    [InlineData("RetryVariants/on-retry-exception-nullable", "retry 1 call 1 TimeoutException, retry 2 call 2 TimeoutException, caught call 3")]
    [InlineData("RetryVariants/durations","wait 0, wait 1, wait 2, caught call 4")]
    [InlineData("WaitAndRetryAsync/on-retry-result", "retry -1 none, retry 0 TimeoutException, result 7")]
    [InlineData("SyncRetry/wait-and-retry", "delay 1, delay 2, delay 3, caught call 4, value 5")]
    [InlineData("SyncRetry/retry-on-retry", "retry 1 call 1, retry 2 call 2, caught call 3")]
    [InlineData("SyncRetry/handle-result", "got -2, got -1, got 0, result 0")]
    [InlineData("SyncRetry/forever", "delay 1, delay 2, delay 3, calls 4")]
    [InlineData("Timeout/token-honoured", "timed out, fast 7")]
    [InlineData("Timeout/token-ignored", "completed, value 4")]
    [InlineData("Timeout/int-seconds", "timed out")]
    [InlineData("Timeout/sync", "value 5")]
    [InlineData("Timeout/typed", "timed out")]
    [InlineData("NoOp/async", "value 3, ran")]
    [InlineData("NoOp/typed", "value 9")]
    [InlineData("NoOp/sync-inline", "value 6")]
    public async Task Proof_BeforeAndAfter_SameTrace(string proof, string expected)
    {
        // Arrange
        string before = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".before.cs"), TestContext.Current.CancellationToken);
        string after = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".after.cs"), TestContext.Current.CancellationToken);

        // Act
        string beforeTrace = await RunAsync(before, PollyReferences.Before);
        string afterTrace = await RunAsync(after, PollyReferences.After);

        // Assert
        beforeTrace.ShouldBe(expected);
        afterTrace.ShouldBe(beforeTrace);
    }

    // Queue row 8: why polly.circuit-breaker is Manual. v7 breaks on the third consecutive failure; the nearest v8 mapping (failure ratio
    // 1.0, minimum throughput 3) keeps the earlier success in its window and does not break on the same calls. If these traces ever
    // become equal, the pattern may be revisited.
    [Fact]
    public async Task CircuitBreaker_V8RatioMapping_BreaksOnDifferentCalls()
    {
        // Arrange
        const string Loop = "foreach (char c in \"FSFFFF\") { try { @CALL@ trace.Add(\"S\"); } catch (BrokenCircuitException) { trace.Add(\"B\"); } catch (InvalidOperationException) { trace.Add(\"F\"); } }";
        const string Head = "using System; using System.Collections.Generic; using System.Threading.Tasks; using Polly; using Polly.CircuitBreaker;\npublic static class Proof { public static async Task<string> RunAsync() { var trace = new List<string>();\n";
        const string Tail = "\nreturn string.Join(\",\", trace); } }";
        string v7 = Head + "var breaker = Policy.Handle<InvalidOperationException>().CircuitBreakerAsync(3, TimeSpan.FromSeconds(1));\n"
            + Loop.Replace("@CALL@", "await breaker.ExecuteAsync(() => { if (c == 'F') throw new InvalidOperationException(); return Task.CompletedTask; });", StringComparison.Ordinal) + Tail;
        string v8 = Head + "var breaker = new ResiliencePipelineBuilder().AddCircuitBreaker(new CircuitBreakerStrategyOptions { ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(), FailureRatio = 1.0, MinimumThroughput = 3, SamplingDuration = TimeSpan.FromSeconds(30), BreakDuration = TimeSpan.FromSeconds(1) }).Build();\n"
            + Loop.Replace("@CALL@", "await breaker.ExecuteAsync(async _ => { if (c == 'F') throw new InvalidOperationException(); await Task.CompletedTask; });", StringComparison.Ordinal) + Tail;

        // Act
        string before = await RunAsync(v7, PollyReferences.Before);
        string after = await RunAsync(v8, PollyReferences.After);

        // Assert
        before.ShouldBe("F,S,F,F,F,B");
        after.ShouldBe("F,S,F,F,F,F");
    }

    [Fact]
    public async Task Proof_WithoutAttemptOffset_DifferentTrace()
    {
        // Arrange: the proof must fail when the 0-based AttemptNumber is passed on as is.
        string after = await File.ReadAllTextAsync(Path.Combine(s_directory, "WaitAndRetryAsync", "local-block-delay.after.cs"), TestContext.Current.CancellationToken);
        string mutated = after.Replace("args.AttemptNumber + 1", "args.AttemptNumber", StringComparison.Ordinal);

        // Act
        string trace = await RunAsync(mutated, PollyReferences.After);

        // Assert
        trace.ShouldBe("delay 0, delay 1, delay 2, caught call 4, calls 4");
    }

    // Council round 8, P4: each wrap proof pins the order, so it would fail if a rewrite reversed it.
    [Theory]
    [InlineData("two-locals", "var policy = outer.WrapAsync(inner);", "var policy = inner.WrapAsync(outer);")]
    [InlineData("three-inline", "Policy.Handle<TimeoutException>().RetryAsync(1, (exception, retryCount) => trace.Add(\"timeout \" + retryCount)),", "Policy.Handle<ArgumentException>().RetryAsync(1, (exception, retryCount) => trace.Add(\"argument \" + retryCount)),")]
    public async Task WrapProof_OrderReversed_DifferentTrace(string proof, string original, string reversed)
    {
        // Arrange
        string before = await File.ReadAllTextAsync(Path.Combine(s_directory, "Wrap", proof + ".before.cs"), TestContext.Current.CancellationToken);
        string swapped = Reverse(before, original, reversed);

        // Act
        string trace = await RunAsync(before, PollyReferences.Before);
        string reversedTrace = await RunAsync(swapped, PollyReferences.Before);

        // Assert
        swapped.ShouldNotBe(before);
        reversedTrace.ShouldNotBe(trace);
    }

    // Swaps the first and last operand (two-locals: the receiver and argument; three-inline: the first and third line).
    private static string Reverse(string code, string original, string reversed)
    {
        const string Marker = "@@SWAP@@";
        return code.Replace(original, Marker, StringComparison.Ordinal)
            .Replace(reversed.TrimEnd(','), original.TrimEnd(','), StringComparison.Ordinal)
            .Replace(Marker, reversed, StringComparison.Ordinal);
    }

    private static async Task<string> RunAsync(string code, IEnumerable<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create(
            "PollyProof" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue(string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        var assembly = Assembly.Load(stream.ToArray());
        MethodInfo run = assembly.GetType("Proof")!.GetMethod("RunAsync")!;
        return await (Task<string>)run.Invoke(null, null)!;
    }
}
