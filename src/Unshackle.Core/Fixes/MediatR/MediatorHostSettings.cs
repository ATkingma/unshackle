namespace Unshackle.Core.Fixes.MediatR;

/// <summary>Extra AddMediator settings for a host next to other hosts (<see cref="MediatorHosts"/>), or why it needs a hand.</summary>
public class MediatorHostSettings
{
    /// <summary>No extra settings: the host is alone in its references.</summary>
    public static MediatorHostSettings None { get; } = new() { Assignments = [] };

    /// <summary>Property and source value per setting, in this order (e.g. <c>GenerateTypesAsInternal</c>, <c>true</c>).</summary>
    public required IReadOnlyList<(string Property, string Value)> Assignments { get; init; }

    /// <summary>Set when the settings cannot be written: the reason for the report.</summary>
    public string? ManualReason { get; init; }
}
