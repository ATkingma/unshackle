using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// A project file as XML plus the original text of every node. Only nodes that changed are written anew; everything else is
// copied from the original text, so formatting (/>, " >, attributes over lines, mixed line endings, comments) stays byte for byte.
internal partial class ProjectFile
{
    private readonly string _text;
    private readonly string _prefix;
    private readonly string _suffix;
    private readonly Dictionary<XNode, Span> _spans = [];

    public ProjectFile(string path, string text)
    {
        Path = path;
        OldText = text;
        _text = text;
        // No newline normalization: whitespace copied from a neighbour keeps its own line ending.
        using var reader = new XmlTextReader(new StringReader(text)) { Normalization = false, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        Document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        XElement root = Document.Root!;
        IXmlLineInfo position = root;
        int lineStart = 0;
        for (int line = 1; line < position.LineNumber; line++)
        {
            lineStart = text.IndexOf('\n', lineStart) + 1;
        }

        // LinePosition points at the element name, one past the '<'.
        int rootStart = lineStart + position.LinePosition - 2;
        int rootEnd = ScanElement(root, rootStart);
        _prefix = text[..rootStart];
        _suffix = text[rootEnd..];
        // CR-only line endings or a reader/scanner mismatch would cut the file wrongly; refuse instead of corrupting it.
        if (ToChange().NewText != text)
        {
            throw new InvalidDataException($"{path} cannot be edited without changing unrelated text; migrate it by hand.");
        }
    }

    public string Path { get; }

    public string OldText { get; }

    public XDocument Document { get; }

    public XNamespace Namespace => Document.Root!.Name.Namespace;

    public ProjectFileChange ToChange() =>
        new() { FilePath = Path, OldText = OldText, NewText = _prefix + Render(Document.Root!) + _suffix };

    private string Render(XNode node)
    {
        if (!_spans.TryGetValue(node, out Span span))
        {
            return node switch
            {
                XElement element => New(element),
                XText text => SecurityElement.Escape(text.Value),
                _ => node.ToString(SaveOptions.DisableFormatting),
            };
        }

        return node switch
        {
            XElement element => RenderElement(element, span),
            XText text when text.Value != span.Value => SecurityElement.Escape(text.Value),
            _ => _text[span.Start..span.End],
        };
    }

    private string RenderElement(XElement element, Span span)
    {
        string startTag = _text[span.Start..span.TagEnd];
        List<XAttribute> attributes = [.. element.Attributes()];
        if (!attributes.Select(a => a.Name).SequenceEqual(span.Attributes!.Select(a => a.Name)))
        {
            throw new InvalidDataException($"{Path} cannot be edited without changing unrelated text; migrate it by hand.");
        }

        if (attributes.Where((a, i) => a.Value != span.Attributes![i].Value).Any())
        {
            // Values change in place; matching the attributes in order skips quoted text that looks like an attribute.
            int index = 0;
            startTag = AttributePattern().Replace(startTag, m =>
            {
                int i = index++;
                if (i >= attributes.Count || attributes[i].Value == span.Attributes![i].Value)
                {
                    return m.Value;
                }

                Group value = m.Groups["value"];
                return m.Value[..(value.Index - m.Index)] + SecurityElement.Escape(attributes[i].Value) + m.Value[(value.Index - m.Index + value.Length)..];
            });
        }

        if (span.TagEnd == span.End)
        {
            return element.Nodes().Any()
                ? throw new InvalidDataException($"{Path} cannot be edited without changing unrelated text; migrate it by hand.")
                : startTag;
        }

        var builder = new StringBuilder(startTag);
        foreach (XNode child in element.Nodes())
        {
            builder.Append(Render(child));
        }

        return builder.Append(_text[span.ContentEnd..span.End]).ToString();
    }

    // shortcut: a new element is written on one line with ' />'; enough for the references and usings this store adds.
    private string New(XElement element)
    {
        string attributes = string.Concat(element.Attributes().Select(a => $" {a.Name.LocalName}=\"{SecurityElement.Escape(a.Value)}\""));
        return element.Nodes().Any()
            ? $"<{element.Name.LocalName}{attributes}>{string.Concat(element.Nodes().Select(Render))}</{element.Name.LocalName}>"
            : $"<{element.Name.LocalName}{attributes} />";
    }

    // Walks the original text next to the parsed element and records where each node starts and ends. Returns the end.
    private int ScanElement(XElement element, int start)
    {
        int tagEnd = SkipTag(start);
        List<(XName Name, string Value)> attributes = [.. element.Attributes().Select(a => (a.Name, a.Value))];

        if (_text[tagEnd - 2] == '/')
        {
            _spans[element] = new Span(start, tagEnd, tagEnd, tagEnd, null, attributes);
            return tagEnd;
        }

        int position = tagEnd;
        using IEnumerator<XNode> nodes = element.Nodes().GetEnumerator();
        while (!_text.AsSpan(position).StartsWith("</"))
        {
            if (!nodes.MoveNext())
            {
                throw Mismatch();
            }

            int end = nodes.Current switch
            {
                XElement child when _text[position] == '<' => ScanElement(child, position),
                XComment when _text.AsSpan(position).StartsWith("<!--") => _text.IndexOf("-->", position, StringComparison.Ordinal) + 3,
                XCData when _text.AsSpan(position).StartsWith("<![CDATA[") => _text.IndexOf("]]>", position, StringComparison.Ordinal) + 3,
                XProcessingInstruction when _text.AsSpan(position).StartsWith("<?") => _text.IndexOf("?>", position, StringComparison.Ordinal) + 2,
                XText and not XCData when _text[position] != '<' => NextTag(position),
                _ => throw Mismatch(),
            };
            if (nodes.Current is not XElement)
            {
                _spans[nodes.Current] = new Span(position, end, end, end, (nodes.Current as XText)?.Value, null);
            }

            position = end;
        }

        if (nodes.MoveNext())
        {
            throw Mismatch();
        }

        int closeEnd = _text.IndexOf('>', position) + 1;
        _spans[element] = new Span(start, tagEnd, position, closeEnd, null, attributes);
        return closeEnd;
    }

    private int NextTag(int position)
    {
        int next = _text.IndexOf('<', position);
        return next < 0 ? throw Mismatch() : next;
    }

    // End of the tag that starts at start, skipping '>' inside quoted attribute values.
    private int SkipTag(int start)
    {
        char quote = '\0';
        for (int i = start + 1; i < _text.Length; i++)
        {
            char c = _text[i];
            if (quote != '\0')
            {
                quote = c == quote ? '\0' : quote;
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == '>')
            {
                return i + 1;
            }
        }

        throw Mismatch();
    }

    private InvalidDataException Mismatch() => new($"{Path} cannot be edited without changing unrelated text; migrate it by hand.");

    [GeneratedRegex("""\s[^\s=<>/"']+\s*=\s*(["'])(?<value>.*?)\1""", RegexOptions.Singleline)]
    private static partial Regex AttributePattern();

    // Start/End: the node in the original text. TagEnd/ContentEnd: end of the start tag and start of the end tag (elements).
    private readonly record struct Span(
        int Start, int TagEnd, int ContentEnd, int End, string? Value, List<(XName Name, string Value)>? Attributes);
}
