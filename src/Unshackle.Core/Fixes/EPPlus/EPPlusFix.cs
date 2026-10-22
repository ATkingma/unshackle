using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus;

/// <summary>EPPlus (LGPL 4.5.3.3, commercial 5+) → ClosedXML (MIT). TAAK-309; council round 8, E1–E6.</summary>
public class EPPlusFix : IFix
{
    // The version our proofs and the cell-compare harness run against (Directory.Packages.props); MIT, no licence check (E5, TAAK-309).
    public const string ClosedXmlVersion = "0.105.1";

    public string Id => "epplus";

    public IReadOnlyList<string> RemovedNamespaces { get; } = [EPPlusSyntax.Namespace];

    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // The package goes per project only when nothing of EPPlus is left there (the Fixer keeps it next to ClosedXML otherwise; general rule, round 8).
    public PackageMigration? Package { get; } = new()
    {
        RemovedPackage = "EPPlus",
        AddedPackages = [new AddedPackage { Name = "ClosedXML", Version = ClosedXmlVersion }],
    };

    // A workbook half on EPPlus and half on ClosedXML does not build (an ExcelWorksheet passed to a migrated method): any Manual finding leaves the solution as it is.
    public bool AllOrNothing => true;

    public IReadOnlyList<IPattern> Patterns { get; } = [new LicenseContextPattern(), new PackagePattern()];
}
