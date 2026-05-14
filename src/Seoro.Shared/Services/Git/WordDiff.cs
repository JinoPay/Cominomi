using System.Text.RegularExpressions;
using Seoro.Shared.Models.Git;

namespace Seoro.Shared.Services.Git;

public static partial class WordDiff
{
    private const int MaxLength = 1024;

    public static (IReadOnlyList<DiffSegment> Old, IReadOnlyList<DiffSegment> New) Compute(string oldText, string newText)
    {
        if (oldText == newText)
        {
            return (
                oldText.Length == 0 ? [] : [new DiffSegment(oldText, false)],
                newText.Length == 0 ? [] : [new DiffSegment(newText, false)]);
        }

        if (oldText.Length > MaxLength || newText.Length > MaxLength)
        {
            return (
                [new DiffSegment(oldText, true)],
                [new DiffSegment(newText, true)]);
        }

        var oldTokens = Tokenize(oldText);
        var newTokens = Tokenize(newText);

        var matched = LongestCommonSubsequence(oldTokens, newTokens);

        return (
            BuildSegments(oldTokens, matched.OldMatched),
            BuildSegments(newTokens, matched.NewMatched));
    }

    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var parts = TokenSplitRegex().Split(text);
        return parts.Where(p => p.Length > 0).ToList();
    }

    private static (bool[] OldMatched, bool[] NewMatched) LongestCommonSubsequence(List<string> a, List<string> b)
    {
        var m = a.Count;
        var n = b.Count;
        var dp = new int[m + 1, n + 1];
        for (var i = 1; i <= m; i++)
        for (var j = 1; j <= n; j++)
        {
            dp[i, j] = a[i - 1] == b[j - 1]
                ? dp[i - 1, j - 1] + 1
                : Math.Max(dp[i - 1, j], dp[i, j - 1]);
        }

        var oldMatched = new bool[m];
        var newMatched = new bool[n];
        var x = m;
        var y = n;
        while (x > 0 && y > 0)
        {
            if (a[x - 1] == b[y - 1])
            {
                oldMatched[x - 1] = true;
                newMatched[y - 1] = true;
                x--;
                y--;
            }
            else if (dp[x - 1, y] >= dp[x, y - 1])
            {
                x--;
            }
            else
            {
                y--;
            }
        }

        return (oldMatched, newMatched);
    }

    private static List<DiffSegment> BuildSegments(List<string> tokens, bool[] matched)
    {
        var segments = new List<DiffSegment>();
        if (tokens.Count == 0) return segments;

        var buffer = tokens[0];
        var bufferChanged = !matched[0];
        for (var i = 1; i < tokens.Count; i++)
        {
            var changed = !matched[i];
            if (changed == bufferChanged)
            {
                buffer += tokens[i];
            }
            else
            {
                segments.Add(new DiffSegment(buffer, bufferChanged));
                buffer = tokens[i];
                bufferChanged = changed;
            }
        }
        segments.Add(new DiffSegment(buffer, bufferChanged));
        return segments;
    }

    [GeneratedRegex(@"(\s+|[^\w\s])")]
    private static partial Regex TokenSplitRegex();
}
