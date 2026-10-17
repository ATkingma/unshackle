using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MediatR;

// Union-find over owners (by file and span, so the copies of a multi-target file are one owner) and the three message kinds.
internal class MediatROwnerGraph
{
    private readonly Dictionary<string, string> _parent = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _messages = new(StringComparer.Ordinal);
    private readonly HashSet<string> _linkedKinds = new(StringComparer.Ordinal);

    public Dictionary<string, MediatROwner> Owners { get; } = new(StringComparer.Ordinal);

    public static string Key(string path, SyntaxNode node) => $"{path}:{node.Span.Start}:{node.Span.End}";

    public MediatROwner Owner(string path, SyntaxNode node)
    {
        string key = Key(path, node);
        if (!Owners.TryGetValue(key, out MediatROwner? owner))
        {
            FileLinePositionSpan lines = node.GetLocation().GetLineSpan();
            owner = new MediatROwner
            {
                Key = key,
                Path = path,
                Span = node.Span,
                StartLine = lines.StartLinePosition.Line + 1,
                EndLine = lines.EndLinePosition.Line + 1,
            };
            Owners[key] = owner;
        }

        return owner;
    }

    public void AddMessage(string key, string kind)
    {
        if (!_messages.TryGetValue(key, out HashSet<string>? kinds))
        {
            _messages[key] = kinds = new(StringComparer.Ordinal);
        }

        kinds.Add(kind);
    }

    // The owner reaches every message of the kind.
    public void Link(string key, string kind)
    {
        _linkedKinds.Add(kind);
        Union(key, kind);
    }

    // After all links: a kind that something reaches as a whole joins all its messages.
    public void JoinKinds()
    {
        foreach ((string key, HashSet<string> kinds) in _messages)
        {
            foreach (string kind in kinds.Where(_linkedKinds.Contains))
            {
                Union(key, kind);
            }
        }
    }

    public string Find(string key)
    {
        string root = key;
        while (_parent.TryGetValue(root, out string? up) && up != root)
        {
            root = up;
        }

        // Path compression: later finds are short.
        while (key != root && _parent.TryGetValue(key, out string? up))
        {
            _parent[key] = root;
            key = up;
        }

        return root;
    }

    public void Union(string a, string b)
    {
        string rootA = Find(a);
        string rootB = Find(b);
        if (rootA != rootB)
        {
            _parent[rootA] = rootB;
        }
    }

    // The owner a finding points at: the outermost one that starts on its line, else the innermost one around it.
    public MediatROwner? OwnerAt(string path, int line)
    {
        List<MediatROwner> around = [.. Owners.Values.Where(o => o.Path == path && o.StartLine <= line && line <= o.EndLine)];
        return around.Where(o => o.StartLine == line).MaxBy(o => o.Span.Length) ?? around.MinBy(o => o.Span.Length);
    }
}
