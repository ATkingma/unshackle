namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>Why the stubs of a mocked IMapper stay (TAAK-311): the text (with file:line) and its typed code, when it has one (council round 10, C1).</summary>
internal class MockReason
{
    public required string Text { get; init; }

    public MockSetUpCode? Code { get; init; }

    public static MockReason Of(string text) => new() { Text = text };

    public static MockReason Of(MockSetUpCode code, string text) => new() { Text = text, Code = code };

    public override string ToString() => Text;
}
