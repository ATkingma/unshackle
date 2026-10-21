using Shouldly;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Xunit;

namespace Unshackle.Tests.Core;

public class AutoMapperFixTests
{
    // TAAK-295: only the typed reason code marks a kept finding; the text prefix alone no longer does.
    [Theory]
    [InlineData(KeptReason.MapKept, "CreateMap<A, B> stays in the profile", true)]
    [InlineData(KeptReason.None, "Stays on AutoMapper: CreateMap<A, B> stays in the profile", false)]
    [InlineData(KeptReason.None, "some other problem", false)]
    public void AllowsPartial_ManualFinding_DependsOnlyOnTheCode(KeptReason code, string reason, bool expected)
    {
        // Arrange
        var finding = new Finding
        {
            FilePath = "A.cs",
            Line = 1,
            FixId = "automapper",
            PatternId = "automapper.kept-map",
            Result = PatternResult.KeptManual(code, reason),
        };

        // Act
        bool allowed = new AutoMapperFix().AllowsPartial(finding);

        // Assert
        allowed.ShouldBe(expected);
    }

    // The report renders a kept finding with the prefix in front of its reason, as before the code existed.
    [Fact]
    public void ReasonOf_KeptManual_PrintsThePrefix()
    {
        // Arrange
        var manual = (ManualResult)PatternResult.KeptManual(KeptReason.CallKept, "this call keeps using IMapper: x");

        // Act
        string rendered = ReportRow.ReasonOf(manual);

        // Assert
        rendered.ShouldBe("Stays on AutoMapper: this call keeps using IMapper: x");
    }
}
