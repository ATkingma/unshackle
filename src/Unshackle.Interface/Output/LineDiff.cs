namespace Unshackle.Interface.Output;

// Line diff via longest common subsequence; only changed lines are returned, removed before added per change.
public static class LineDiff
{
    public static IReadOnlyList<DiffLine> Compute(string oldText, string newText)
    {
        string[] a = oldText.ReplaceLineEndings("\n").Split('\n');
        string[] b = newText.ReplaceLineEndings("\n").Split('\n');

        // Rewrites are local: skip the common start and end so the LCS table stays small.
        int start = 0;
        while (start < a.Length && start < b.Length && a[start] == b[start])
        {
            start++;
        }

        int end = 0;
        while (end < a.Length - start && end < b.Length - start && a[^(end + 1)] == b[^(end + 1)])
        {
            end++;
        }

        int n = a.Length - start - end;
        int m = b.Length - start - end;
        int[][] lcs = [.. Enumerable.Range(0, n + 1).Select(_ => new int[m + 1])];
        for (int i = n - 1; i >= 0; i--)
        {
            for (int j = m - 1; j >= 0; j--)
            {
                lcs[i][j] = a[start + i] == b[start + j] ? lcs[i + 1][j + 1] + 1 : Math.Max(lcs[i + 1][j], lcs[i][j + 1]);
            }
        }

        var lines = new List<DiffLine>();
        var added = new List<DiffLine>();
        int x = 0;
        int y = 0;
        while (x < n || y < m)
        {
            if (x < n && y < m && a[start + x] == b[start + y])
            {
                lines.AddRange(added);
                added.Clear();
                x++;
                y++;
            }
            else if (y == m || (x < n && lcs[x + 1][y] >= lcs[x][y + 1]))
            {
                lines.Add(new DiffLine { Kind = DiffKind.Removed, LineNumber = start + x + 1, Text = a[start + x] });
                x++;
            }
            else
            {
                added.Add(new DiffLine { Kind = DiffKind.Added, LineNumber = start + y + 1, Text = b[start + y] });
                y++;
            }
        }

        lines.AddRange(added);
        return lines;
    }
}
