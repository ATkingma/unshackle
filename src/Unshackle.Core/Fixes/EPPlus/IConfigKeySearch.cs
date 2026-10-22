namespace Unshackle.Core.Fixes.EPPlus;

/// <summary>
/// Finds a configuration key in the config files of a solution (council round 11, E11: <c>IsWorksheets1Based</c>), without reading its value.
/// Implemented in DataAccess; <see cref="EPPlusFix"/> gets it through its constructor.
/// </summary>
public interface IConfigKeySearch
{
    /// <summary>
    /// Every place the key occurs in a config file (<c>*.config</c>, <c>appsettings*.json</c> and the like) under the solution's directory,
    /// as <c>file:line</c>. Empty when it occurs nowhere.
    /// </summary>
    /// <param name="solutionDirectory">The directory of the solution file.</param>
    /// <param name="key">The text to look for, for example <c>IsWorksheets1Based</c>.</param>
    IReadOnlyList<string> Find(string solutionDirectory, string key);
}
