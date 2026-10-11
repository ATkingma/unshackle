using Shouldly;
using Unshackle.Interface.Output;
using Xunit;

namespace Unshackle.Tests.Interface.Output;

public class ConsoleTextTests
{
    [Fact]
    public void Sanitize_EscapeSequence_ReplacesControlCharacters()
    {
        // Arrange
        const string Text = "x\u001b[2Jy\r\n\tz";

        // Act
        string result = ConsoleText.Sanitize(Text);

        // Assert
        result.ShouldBe("x�[2Jy��\tz");
    }

    [Fact]
    public void Sanitize_BidiAndZeroWidth_ReplacesFormatCharacters()
    {
        // Arrange
        const string Text = "a‮b⁦c​d";

        // Act
        string result = ConsoleText.Sanitize(Text);

        // Assert
        result.ShouldBe("a�b�c�d");
    }
}
