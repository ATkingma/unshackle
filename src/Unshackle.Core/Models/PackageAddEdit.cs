namespace Unshackle.Core.Models;

// A PackageReference added to the project file (with a <PackageVersion> under central package management). Nothing to do when the
// project or a file it imports already declares it.
public class PackageAddEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    public required string Version { get; init; }

    // TAAK-329, council round 16 G10: set when the package only came transitively with this removed package. The version is the resolved
    // one, so the build resolves what it did before; a central PackageVersion with another version is then Manual instead of kept.
    public string? KeepsTransitiveOf { get; init; }

    // Council round 17 T2, round 19 T5: the user projects whose code uses the package, in solution order; set with KeepsTransitiveOf.
    public IReadOnlyList<string> KeptFor { get; init; } = [];

    // Council round 19 T5: a row names at most this many users, then "and N more"; the full list goes in the detailed report.
    internal const int MaxUsersInRow = 3;

    // `A`, `B`, `C` and 2 more.
    internal static string UserList(IReadOnlyList<string> users) =>
        users.Count == 0
            ? "`?`"
            : string.Join(", ", users.Take(MaxUsersInRow).Select(u => $"`{u}`")) + (users.Count > MaxUsersInRow ? $" and {users.Count - MaxUsersInRow} more" : "");

    // Council round 17 T1: a pin is not checked against the new package's closure, so the row starts with the NU1605 hint and its remedy.
    public override string Describe() => KeepsTransitiveOf is string removed
        ? $"may conflict (NU1605): PackageReference {Name} pinned to {Version}, kept for {UserList(KeptFor)} (it came transitively with {removed}). "
            + $"If restore reports NU1605 for {Name}, raise the pin"
        : $"PackageReference {Name} {Version} added";
}
