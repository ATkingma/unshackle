using System.Xml;
using System.Xml.Linq;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// A project file as XML, plus the text XDocument cannot hold (around the root, line endings), so unchanged parts stay byte for byte.
internal class ProjectFile
{
    private readonly string _prefix;
    private readonly string _suffix;
    private readonly bool _crlf;

    public ProjectFile(string path, string text)
    {
        Path = path;
        OldText = text;
        Document = XDocument.Parse(text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        XElement root = Document.Root!;
        IXmlLineInfo position = root;
        int lineStart = 0;
        for (int line = 1; line < position.LineNumber; line++)
        {
            lineStart = text.IndexOf('\n', lineStart) + 1;
        }

        // LinePosition points at the element name, one past the '<'.
        int rootStart = lineStart + position.LinePosition - 2;
        int closingTag = text.LastIndexOf("</" + root.Name.LocalName, StringComparison.Ordinal);
        int rootEnd = text.IndexOf('>', closingTag < rootStart ? rootStart : closingTag) + 1;
        _prefix = text[..rootStart];
        _suffix = text[rootEnd..];
        _crlf = text.Contains("\r\n", StringComparison.Ordinal);
        // A "</Project>" in a trailing comment or CR-only line endings would cut the file wrongly; refuse instead of corrupting it.
        if (ToChange().NewText != text)
        {
            throw new InvalidDataException($"{path} cannot be edited without changing unrelated text; migrate it by hand.");
        }
    }

    public string Path { get; }

    public string OldText { get; }

    public XDocument Document { get; }

    public XNamespace Namespace => Document.Root!.Name.Namespace;

    public ProjectFileChange ToChange()
    {
        // The reader turned CRLF into LF; the writer may use Environment.NewLine.
        string root = Document.Root!.ToString(SaveOptions.DisableFormatting).ReplaceLineEndings(_crlf ? "\r\n" : "\n");
        return new ProjectFileChange { FilePath = Path, OldText = OldText, NewText = _prefix + root + _suffix };
    }
}
