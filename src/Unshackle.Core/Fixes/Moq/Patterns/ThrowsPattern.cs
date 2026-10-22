namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Setup(x => x.M(..)).Throws(ex);` → `m.Object.M(..).Throws(ex);` (NSubstitute.ExceptionExtensions), ThrowsAsync likewise; a void member
// becomes `m.Object.When(x => x.M(..)).Do(_ => throw ex);` (moq-queue.md row 6). The plan is shared with moq.setup-returns.
public class ThrowsPattern : SetupReturnsPattern
{
    public override string Id => "moq.throws";

    public override string Description => "m.Setup(..).Throws(ex)/ThrowsAsync(ex) → m.M(..).Throws(ex)/ThrowsAsync(ex); void → m.When(..).Do(_ => throw ex)";

    protected override bool FindsThrows => true;
}
