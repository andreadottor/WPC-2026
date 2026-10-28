namespace Dottor.WPC2026.Web.Services.Ai;

public enum DiffKind { Same, Added, Removed }

public readonly record struct DiffPart(DiffKind Kind, string Text);

/// <summary>Diff a livello di parola (LCS), sufficiente per evidenziare le proposte dell'agente.</summary>
public static class TextDiff
{
    public static IReadOnlyList<DiffPart> Compute(string? oldText, string? newText)
    {
        var a = Tokenize(oldText);
        var b = Tokenize(newText);
        var lcs = new int[a.Length + 1, b.Length + 1];

        for (var i = a.Length - 1; i >= 0; i--)
        {
            for (var j = b.Length - 1; j >= 0; j--)
            {
                lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var parts = new List<DiffPart>();
        int x = 0, y = 0;
        while (x < a.Length || y < b.Length)
        {
            if (x < a.Length && y < b.Length && a[x] == b[y])
            {
                Append(parts, DiffKind.Same, a[x]); x++; y++;
            }
            else if (y < b.Length && (x == a.Length || lcs[x, y + 1] >= lcs[x + 1, y]))
            {
                Append(parts, DiffKind.Added, b[y]); y++;
            }
            else
            {
                Append(parts, DiffKind.Removed, a[x]); x++;
            }
        }
        return parts;
    }

    private static string[] Tokenize(string? text) =>
        string.IsNullOrEmpty(text) ? [] : System.Text.RegularExpressions.Regex.Split(text, @"(\s+)").Where(t => t.Length > 0).ToArray();

    private static void Append(List<DiffPart> parts, DiffKind kind, string token)
    {
        if (parts.Count > 0 && parts[^1].Kind == kind)
        {
            parts[^1] = parts[^1] with { Text = parts[^1].Text + token };
        }
        else
        {
            parts.Add(new DiffPart(kind, token));
        }
    }
}
