using Shouldly;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core;

public class MediatRFixPackageTests
{
    [Theory]
    [InlineData("MSG0001")] // false multiple handlers for a void handler behind an own marker in a referenced project (TAAK-212)
    [InlineData("MSG0003")] // generic decorator skipped (TAAK-193)
    [InlineData("MSG0005")] // message without handler (TAAK-136)
    public void Package_SourceGenerator_KeepsGeneratorWarningAsWarning(string id)
    {
        // Arrange
        MediatRFix fix = new();

        // Act
        AddedPackage generator = fix.Package.AddedPackages.Single(p => p.Name == "Mediator.SourceGenerator");

        // Assert
        generator.WarningsNotAsErrors.ShouldContain(id);
    }
}
