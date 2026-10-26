using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// The uses of a mock other than .Object that a pattern of this fix rewrites (one rule per queue row). Null: the use is rewritten;
// otherwise why the mock stays Moq. A rule adds the members it sets up for every argument to setUp (no pin for them).
public static class MockUseRules
{
    public static MoqBlocker? Blocker(ExpressionSyntax use, SemanticModel model, MockUseContext context, bool pendingUses)
    {
        // Row 5: SetupProperty / SetupAllProperties; moq.create rewrites them itself, so they count in both modes.
        if (use.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax tracking } trackingAccess
            && trackingAccess.Expression == use && PropertySyntax.IsTracking(tracking, model))
        {
            return PropertySyntax.Record(tracking, model, context);
        }

        if (use.Parent is MemberAccessExpressionSyntax { Name.Identifier.Text: "SetupSet" })
        {
            return MoqBlocker.Of(MoqReason.PropertySetup, "SetupSet configures a setter call; NSubstitute has no equivalent setup (use VerifySet or a property that remembers)");
        }

        context.FirstConfiguration = Math.Min(context.FirstConfiguration, use.SpanStart);

        // Row 3: m.Setup(..).Returns(v);
        if (pendingUses && use.Parent is MemberAccessExpressionSyntax { Name.Identifier.Text: "Setup" or "SetupGet", Parent: InvocationExpressionSyntax setup } access
            && access.Expression == use && SetupReturnsSyntax.IsSetup(setup, model) && SetupReturnsSyntax.Statement(setup) is not null)
        {
            MockCall plan = SetupReturnsSyntax.Plan(setup, model);
            if (plan is { Blocker: null, ForAnyArguments: true, Member: { } member })
            {
                context.SetUp.Add(member);
            }

            return plan.Blocker;
        }

        // Row 11 (council M2): m.SetupSequence(..).Returns(a).Returns(b);
        if (pendingUses && use.Parent is MemberAccessExpressionSyntax { Name.Identifier.Text: "SetupSequence", Parent: InvocationExpressionSyntax sequence } sequenceAccess
            && sequenceAccess.Expression == use && SetupSequenceSyntax.IsSequence(sequence, model) && SetupSequenceSyntax.Statement(sequence) is not null)
        {
            MockCall plan = SetupSequenceSyntax.Plan(sequence, model);
            if (plan is { Blocker: null, ForAnyArguments: true, Member: { } member })
            {
                context.SetUp.Add(member);
            }

            return plan.Blocker;
        }

        // Row 10 (council M4): m.Verify(); m.VerifyAll(); m.VerifyNoOtherCalls();
        if (pendingUses && use.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax verifyAll } verifyAllAccess
            && verifyAllAccess.Expression == use && VerifyAllSyntax.IsVerifyAll(verifyAll, model))
        {
            return VerifyAllSyntax.Plan(verifyAll, model).Blocker;
        }

        // Row 4: m.Verify(x => .., Times);
        if (pendingUses && use.Parent is MemberAccessExpressionSyntax { Name.Identifier.Text: "Verify" or "VerifyGet" or "VerifySet", Parent: InvocationExpressionSyntax verify } verifyAccess
            && verifyAccess.Expression == use && VerifySyntax.IsVerify(verify, model))
        {
            return VerifySyntax.Plan(verify, model).Blocker;
        }

        // Row 13: uses with no proven NSubstitute form, each with its own reason code.
        if (use.Parent is MemberAccessExpressionSyntax { Name.Identifier.Text: var member2 } && Unsupported(member2) is { } unsupported)
        {
            return unsupported;
        }

        string shown = (use.Parent as MemberAccessExpressionSyntax)?.ToString() ?? use.Parent?.ToString() ?? use.ToString();
        return MoqBlocker.Of(MoqReason.MockUsedBeyondObject, $"{use} is used as a Mock<T> (`{Shorten(shown)}`); that use needs a later moq-queue.md row");
    }

    public static MoqBlocker? Unsupported(string member) => member switch
    {
        "Protected" => MoqBlocker.Of(MoqReason.ProtectedMember, "Protected() reaches a non-public member; NSubstitute cannot configure it. For an HttpMessageHandler write a small fake handler (council round 8, M3)"),
        "As" => MoqBlocker.Of(MoqReason.MockAs, "As<T>() adds an interface to the mock; NSubstitute needs Substitute.For<A, B>() at creation"),
        "Invocations" => MoqBlocker.Of(MoqReason.MockInvocations, "Invocations lists the calls the mock saw; NSubstitute has ReceivedCalls() with another shape"),
        "Reset" => MoqBlocker.Of(MoqReason.MockReset, "Reset() clears setups and calls; ClearReceivedCalls() clears only the calls (not proven the same)"),
        "Raise" or "RaiseAsync" => MoqBlocker.Of(MoqReason.MockRaise, "Raise(..) fires an event; Raise.EventWith(..) is not proven the same"),
        _ => null,
    };

    private static string Shorten(string text)
    {
        string line = text.ReplaceLineEndings(" ");
        return line.Length <= 60 ? line : line[..57] + "...";
    }
}
