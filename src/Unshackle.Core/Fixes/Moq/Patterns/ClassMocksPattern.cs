using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// moq-queue.md row 9: `new Mock<C>(args) { CallBase = true }` + .Object → `Substitute.ForPartsOf<C>(args)`, Manual for every other class or delegate
// mock. The rewrite is moq.create's (same analysis, same declaration/.Object handling); only the id and the mocks it owns differ.
public class ClassMocksPattern : CreatePattern
{
    public override string Id => "moq.class-mocks";

    public override string Description => "new Mock<C>(args) { CallBase = true } + .Object → Substitute.ForPartsOf<C>(args); other class and delegate mocks Manual";

    protected override bool Owns(ITypeSymbol? mocked) => mocked is { TypeKind: TypeKind.Class or TypeKind.Delegate };
}
