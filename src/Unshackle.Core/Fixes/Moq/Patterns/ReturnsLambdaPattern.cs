namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Setup(x => x.M(..)).Returns((int id) => id * 2);` → `m.Object.M(..).Returns(ci => ci.ArgAt<int>(0) * 2);` (moq-queue.md row 7);
// ReturnsAsync with an expression-bodied function wraps the body in Task.FromResult. The plan is shared with moq.setup-returns.
public class ReturnsLambdaPattern : SetupReturnsPattern
{
    public override string Id => "moq.returns-lambda";

    public override string Description => "m.Setup(..).Returns((a, b) => ..) → m.M(..).Returns(ci => .. ci.ArgAt<A>(0) ..)";

    protected override SetupKind Finds => SetupKind.ReturnsLambda;
}
