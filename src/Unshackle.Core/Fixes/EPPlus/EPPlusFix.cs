using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus;

/// <summary>EPPlus (LGPL 4.5.3.3, commercial 5+) → ClosedXML (MIT). TAAK-309; council round 8, E1–E6.</summary>
public class EPPlusFix : IFix, IProjectFileMigration, IPartialFix
{
    // The version our proofs and the cell-compare harness run against (Directory.Packages.props); MIT, no licence check (E5, TAAK-309).
    public const string ClosedXmlVersion = "0.105.1";

    public EPPlusFix()
        : this(null, null, null)
    {
    }

    public EPPlusFix(IProjectProperties? projectProperties, IConfigKeySearch? configSearch)
        : this(projectProperties, configSearch, null)
    {
    }

    // Council round 11, E10/E11: IsPackable as MSBuild sees it (R1) and a search of the solution's config files decide a 4.x worksheet index.
    // TAAK-319: the restored assets decide the E7 pin; without a reader the references of the project are the only proxy (no pin).
    public EPPlusFix(IProjectProperties? projectProperties, IConfigKeySearch? configSearch, IPackageAssetsReader? assetsReader)
    {
        _assetsReader = assetsReader;
        Patterns = [new LicenseContextPattern(), new StylesPattern(), new LayoutPattern(), new DimensionPattern(), new LoadFromPattern(), new PackagePattern(projectProperties, configSearch), new FlowTypesPattern(), new UnsupportedPattern()];
    }

    private readonly IPackageAssetsReader? _assetsReader;

    public string Id => "epplus";

    public IReadOnlyList<string> RemovedNamespaces { get; } =
        [EPPlusSyntax.Namespace, "OfficeOpenXml.Style", "OfficeOpenXml.Drawing", "OfficeOpenXml.Drawing.Chart", "OfficeOpenXml.Table", "OfficeOpenXml.DataValidation", "OfficeOpenXml.ConditionalFormatting"];

    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // The package goes per project only when nothing of EPPlus is left there (the Fixer keeps it next to ClosedXML otherwise; general rule, round 8).
    public PackageMigration? Package { get; } = new()
    {
        RemovedPackage = "EPPlus",
        AddedPackages = [new AddedPackage { Name = "ClosedXML", Version = ClosedXmlVersion }],
    };

    // A workbook half on EPPlus and half on ClosedXML does not build (an ExcelWorksheet passed to a migrated method): any Manual finding
    // keeps its whole project on EPPlus, and the rest moves per project (council round 13, E17); with no safe plan the solution stays as it is.
    public bool AllOrNothing => true;

    public Task<PartialPlan?> PlanPartialAsync(Solution solution, IReadOnlyList<Finding> roots, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(roots);
        return EPPlusProjects.PlanAsync(solution, roots, cancellationToken);
    }

    public IReadOnlyList<IPattern> Patterns { get; }

    private static readonly Version s_two = new(2, 0);

    // E7: the SixLabors.Fonts pin below 2.0, only for a project that declares EPPlus itself, with current assets that show nothing needing 2.0 (else the warning stays).
    public IReadOnlyList<ProjectFileEdit> EditsFor(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        PackageAssets? assets = Assets(project);
        return assets is { State: AssetsState.Current }
            && assets.Requirements.Any(r => r.RequiredBy == "project" && string.Equals(r.Package, "EPPlus", StringComparison.OrdinalIgnoreCase))
            && !assets.RequiresAtLeast(FontsLicence.Package, s_two)
            ? [new PackageAddEdit { Name = FontsLicence.Package, Version = FontsLicence.PinVersion }]
            : [];
    }

    // E7: a project that already resolves SixLabors.Fonts 2.x would pull it into ClosedXML; that blocks the project (council round 10, C1/G4).
    public IReadOnlyList<ManualResult> ProjectBlockers(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        PackageAssets? assets = Assets(project);
        Version? fonts = assets is { State: AssetsState.Current }
            ? assets.ResolvesAtLeast(FontsLicence.Package, s_two) ? PackageRequirement.ParseVersion(assets.Resolved[FontsLicence.Package]) : null
            : FontsLicence.Resolved(project);
        return fonts is { Major: >= 2 }
            ? [(ManualResult)PatternResult.Manual(ProjectBlockingReason.LicensedDependency,
                $"{project.Name} resolves SixLabors.Fonts {fonts.ToString(3)}, which is under the Six Labors Split License; ClosedXML would use it. "
                + "Pin SixLabors.Fonts below 2.0 (or license it) and run the fix again.")]
            : [];
    }

    private PackageAssets? Assets(Project project) => _assetsReader is not null && project.FilePath is string path ? _assetsReader.Read(path) : null;
}
