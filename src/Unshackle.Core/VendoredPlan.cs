using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// What vendoring the packages of a fix changes: new source documents, the files to write, the package swap and the report rows.
internal class VendoredPlan
{
    public static VendoredPlan Empty { get; } = new() { Documents = [], Files = [], Migrations = [], Findings = [] };

    public required IReadOnlyList<(ProjectId Project, string FilePath, string Text)> Documents { get; init; }

    public required IReadOnlyList<ProjectFileChange> Files { get; init; }

    public required IReadOnlyList<ProjectMigration> Migrations { get; init; }

    public required IReadOnlyList<Finding> Findings { get; init; }

    // The fixed solution with the vendored source in it, so it shows what the files on disk will be.
    public Solution AddTo(Solution solution)
    {
        foreach ((ProjectId project, string filePath, string text) in Documents)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(project), Path.GetFileName(filePath), SourceText.From(text), filePath: filePath);
        }

        return solution;
    }
}
