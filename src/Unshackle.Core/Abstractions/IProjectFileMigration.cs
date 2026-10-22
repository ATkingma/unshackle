using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>
/// The non-C# part of a fix (Strategy, council round 8, S3): exact typed edits of project and config files. Implemented next to
/// <see cref="IFix"/>, like <see cref="IPartialFix"/>. The Fixer applies them after the document pass, per project, and only when
/// that project's code has no blocking Manual finding. Patterns never write files.
/// </summary>
public interface IProjectFileMigration
{
    /// <summary>The edits for one project, in order; empty leaves it alone. An edit with nothing to do (for example a package the project does not declare) is skipped.</summary>
    /// <param name="project">A C# project of the original solution that lives in a file.</param>
    IReadOnlyList<ProjectFileEdit> EditsFor(Project project);
}
