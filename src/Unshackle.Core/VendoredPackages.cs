using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// Puts the source of a package that builds on the old library (IFix.VendoredPackages) into the solution, in the one project
// every user of the package references, and plans the swap of the package for the packages it depended on (TAAK-194).
internal static class VendoredPackages
{
    // Rows for the files of a vendored package.
    public const string PatternId = "vendored-package";

    public static async Task<VendoredPlan> PlanAsync(Solution original, IFix fix, CancellationToken cancellationToken)
    {
        if (fix.VendoredPackages.Count == 0)
        {
            return VendoredPlan.Empty;
        }

        List<(ProjectId, string, string)> documents = [];
        List<ProjectFileChange> files = [];
        List<ProjectMigration> migrations = [];
        List<Finding> findings = [];
        foreach (VendoredPackage package in fix.VendoredPackages)
        {
            List<(Project Project, Version Version)> users = [];
            foreach (Project project in original.Projects.Where(p => p.Language == LanguageNames.CSharp))
            {
                Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
                AssemblyIdentity? identity = compilation?.ReferencedAssemblyNames.FirstOrDefault(a => a.Name == package.Name);
                if (identity is not null)
                {
                    users.Add((project, identity.Version));
                }
            }

            if (users.Count == 0)
            {
                continue;
            }

            List<string> userPaths = [.. users.Select(u => Key(u.Project)).Distinct(StringComparer.Ordinal)];
            Version used = users[0].Version;
            string version = $"{used.Major}.{used.Minor}.{used.Build}";
            VendoredVersion? source = package.Versions.FirstOrDefault(v => v.Version == version);
            if (source is null)
            {
                findings.AddRange(userPaths.Select(p => Manual(fix, p,
                    $"{package.Name} {version} builds on the old library and Unshackle has no source of this version to put in its place " +
                    $"(it has {string.Join(", ", package.Versions.Select(v => v.Version))}). Move to one of those versions, or replace the package by hand.")));
                continue;
            }

            Project? root = Root(original, [.. users.Select(u => u.Project)]);
            if (root is null)
            {
                findings.AddRange(userPaths.Select(p => Manual(fix, p,
                    $"{package.Name} is used by projects that do not all reference one of them ({string.Join(", ", users.Select(u => u.Project.Name).Distinct(StringComparer.Ordinal))}), " +
                    "so there is no single project for its source. Reference the package from one shared project, or replace it by hand.")));
                continue;
            }

            string folder = Path.Combine(Path.GetDirectoryName(root.FilePath) ?? string.Empty, package.Folder);
            if (original.Projects.SelectMany(p => p.Documents).Any(d => d.FilePath is string path && IsUnder(path, folder)))
            {
                findings.AddRange(userPaths.Select(p => Manual(fix, p,
                    $"{folder} already has source files, so the source of {package.Name} cannot go there. Move them, or replace the package by hand.")));
                continue;
            }

            string newLine = await NewLineAsync(root, cancellationToken);
            List<Project> rootTargets = [.. original.Projects.Where(p => Key(p) == Key(root))];
            foreach (VendoredFile file in source.Files)
            {
                string path = Path.Combine(folder, file.Name);
                string text = (package.Header.Replace("{version}", version, StringComparison.Ordinal) + file.Text).ReplaceLineEndings(newLine);
                documents.AddRange(rootTargets.Select(t => (t.Id, path, text)));
                files.Add(new ProjectFileChange { FilePath = path, OldText = string.Empty, NewText = text, Creates = true });
                findings.Add(new Finding { FilePath = path, Line = 0, FixId = fix.Id, PatternId = PatternId, Result = PatternResult.Fixed(Array.Empty<SyntaxNode>()) });
            }

            files.AddRange(package.ExtraFiles.Select(f => new ProjectFileChange
            {
                FilePath = Path.Combine(folder, f.Name), OldText = string.Empty, NewText = f.Text.ReplaceLineEndings(newLine), Creates = true,
            }));

            if (root.FilePath is null)
            {
                continue;
            }

            // The root swaps the package for what it depended on (they no longer come in through it); the others only lose it,
            // the root's references reach them through their project reference. Root first: it swaps a shared <PackageVersion> in place.
            migrations.Add(Migration(root.FilePath, new PackageMigration
            {
                RemovedPackage = package.Name,
                AddedPackages = [.. source.Dependencies.Select(d => Resolved(root, d))],
            }));
            migrations.AddRange(userPaths.Where(p => p != root.FilePath).Select(p => Migration(p, new PackageMigration
            {
                RemovedPackage = package.Name, AlsoRemovedPackages = [package.Name], AddedPackages = [],
            })));
        }

        return new VendoredPlan { Documents = documents, Files = files, Migrations = migrations, Findings = findings };
    }

    // The user that every other user references, directly or through other projects. A multi-target project is one project.
    private static Project? Root(Solution solution, List<Project> users)
    {
        ProjectDependencyGraph graph = solution.GetProjectDependencyGraph();
        return users.FirstOrDefault(root => users.All(u => Key(u) == Key(root)
            || graph.GetProjectsThatThisProjectTransitivelyDependsOn(u.Id).Any(d => Key(solution.GetProject(d)!) == Key(root))));
    }

    private static string Key(Project project) => project.FilePath ?? project.Id.Id.ToString();

    private static bool IsUnder(string path, string folder) =>
        path.StartsWith(folder.TrimEnd('/', '\\') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(folder.TrimEnd('/', '\\') + '/', StringComparison.OrdinalIgnoreCase);

    // The line ending the project's own source uses; "\n" when it has none.
    private static async Task<string> NewLineAsync(Project project, CancellationToken cancellationToken)
    {
        foreach (Document document in project.Documents)
        {
            string text = (await document.GetTextAsync(cancellationToken)).ToString();
            if (text.Contains('\n', StringComparison.Ordinal))
            {
                return text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            }
        }

        return "\n";
    }

    // The version the project resolved, from the NuGet cache path of its reference (<id>/<version>/lib/...): pinning the package's
    // own lower minimum would downgrade it (NU1605). Not seen (in-memory, other layout): the version the package asked for.
    private static AddedPackage Resolved(Project project, AddedPackage dependency)
    {
        foreach (PortableExecutableReference reference in project.MetadataReferences.OfType<PortableExecutableReference>())
        {
            string[] parts = (reference.FilePath ?? string.Empty).Split('/', '\\');
            for (int i = 0; i + 2 < parts.Length; i++)
            {
                if (string.Equals(parts[i], dependency.Name, StringComparison.OrdinalIgnoreCase) && parts[i + 2] is "lib" or "ref")
                {
                    return new AddedPackage { Name = dependency.Name, Version = parts[i + 1] };
                }
            }
        }

        return dependency;
    }

    private static ProjectMigration Migration(string projectPath, PackageMigration package) => new()
    {
        ProjectPath = projectPath, Package = package, KeepRemovedPackage = false, RemovedNamespaces = [], AddedNamespaces = [],
    };

    private static Finding Manual(IFix fix, string projectPath, string reason) => new()
    {
        FilePath = projectPath, Line = 0, FixId = fix.Id, PatternId = PatternId, Result = PatternResult.Manual(reason),
    };
}
