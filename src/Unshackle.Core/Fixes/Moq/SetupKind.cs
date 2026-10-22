namespace Unshackle.Core.Fixes.Moq;

// Which pattern rewrites a Setup statement: the step that decides its shape (moq-queue.md rows 3, 7, 6, 8).
public enum SetupKind
{
    Returns,
    ReturnsLambda,
    Throws,
    Callback,
}
