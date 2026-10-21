namespace Unshackle.Core.Fixes.MediatR.Patterns;

internal class NotificationClosedTypes
{
    public HashSet<string> Arguments { get; } = new(StringComparer.Ordinal);

    public HashSet<string> Constrained { get; } = new(StringComparer.Ordinal);

    public bool MakesGenericType { get; set; }

    public string? Subclass { get; set; }
}
