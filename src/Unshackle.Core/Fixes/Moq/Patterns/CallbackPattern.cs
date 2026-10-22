namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Setup(x => x.M(..)).Returns(v).Callback((int id) => ..);` → `m.Object.M(..).Returns(v).AndDoes(ci => .. ci.ArgAt<int>(0) ..);`;
// a Callback without a value → `m.Object.When(x => x.M(..)).Do(ci => ..);` (moq-queue.md row 8). The plan is shared with moq.setup-returns.
public class CallbackPattern : SetupReturnsPattern
{
    public override string Id => "moq.callback";

    public override string Description => "m.Setup(..).Callback(f)[.Returns(v)] → m.M(..).Returns(v).AndDoes(ci => ..) or m.When(..).Do(ci => ..)";

    protected override SetupKind Finds => SetupKind.Callback;
}
