namespace Unshackle.Interface.Output;

public static class ConsoleText
{
    // Source code and paths are untrusted: a raw ESC or other control character could drive the terminal.
    public static string Sanitize(string text) =>
        string.Create(text.Length, text, static (span, source) =>
        {
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                span[i] = char.IsControl(c) && c != '\t' ? '�' : c;
            }
        });
}
