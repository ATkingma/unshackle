namespace Unshackle.Core.Models;

/// <summary>One requirement on a package in the assets file (TAAK-319): who requires it and the version range as NuGet wrote it.</summary>
public class PackageRequirement
{
    /// <summary>The required package id.</summary>
    public required string Package { get; init; }

    /// <summary>The project itself (direct) or the requiring package as <c>Id/Version</c>.</summary>
    public required string RequiredBy { get; init; }

    /// <summary>The range as written, for example <c>1.0.0</c>, <c>[2.0.0, )</c> or <c>(, 3.0]</c>.</summary>
    public required string Range { get; init; }

    /// <summary>The lowest version the range allows; null when it cannot be read. An open lower end is 0.0.</summary>
    public Version? LowerBound
    {
        get
        {
            string range = Range.Trim();
            if (range.StartsWith('(') || range.StartsWith('['))
            {
                string lower = range[1..].Split(',')[0].Trim().TrimEnd(']', ')');
                return lower.Length == 0 ? new Version(0, 0) : ParseVersion(lower);
            }

            return ParseVersion(range);
        }
    }

    /// <summary>A NuGet version without its prerelease or metadata part, or null.</summary>
    public static Version? ParseVersion(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string plain = text.Trim().Split('-', '+')[0];
        return Version.TryParse(plain.Contains('.', StringComparison.Ordinal) ? plain : plain + ".0", out Version? version) ? version : null;
    }
}
