using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Unshackle.Tests.Helpers;

public class TestSolutionTests
{
    // Parallel tests sharing a SolutionId overwrote each other's MapOwners plan (TAAK-284).
    [Fact]
    public void NewEmpty_CalledTwice_GivesDifferentSolutionIds()
    {
        // Arrange
        Solution first = TestSolution.NewEmpty();

        // Act
        Solution second = TestSolution.NewEmpty();

        // Assert
        first.Id.ShouldNotBe(second.Id);
    }
}
