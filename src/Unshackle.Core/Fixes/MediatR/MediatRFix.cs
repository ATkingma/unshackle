using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR;

public class MediatRFix : IFix
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): latest stable, MIT.
    public const string MediatorVersion = "3.0.2";

    public string Id => "mediatr";

    public IReadOnlyList<string> RemovedNamespaces { get; } = ["MediatR"];

    public IReadOnlyList<string> AddedNamespaces { get; } = ["Mediator"];

    // The source generator writes AddMediator and the dispatcher; without it the abstractions do nothing.
    public PackageMigration Package { get; } = new()
    {
        RemovedPackage = "MediatR",
        AddedPackages =
        [
            new() { Name = "Mediator.Abstractions", Version = MediatorVersion },
            new() { Name = "Mediator.SourceGenerator", Version = MediatorVersion },
        ],
    };

    public IReadOnlyList<IPattern> Patterns { get; } = [];
}
