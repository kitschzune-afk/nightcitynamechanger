using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Ncnc;
public static class Replacement
{
    // Word boundaries protect words/identifiers. Compound names and abbreviations are checked separately.
    private static readonly Dictionary<string, Regex> Patterns = Characters.All.ToDictionary(c => c.Id, c => Pattern(c.Aliases));
    private static Regex Pattern(IEnumerable<string> aliases) => new(
        @"(?<![\p{L}\p{M}\p{N}_])(?:" + string.Join("|", aliases.SelectMany(a => new[] { a, a.ToUpperInvariant() }).Distinct().OrderByDescending(a => a.Length).Select(Regex.Escape)) + @")(?![\p{L}\p{M}\p{N}_])",
        RegexOptions.Compiled);
    private static readonly Regex EscapedWhitespace = new(@"\\[nrt]", RegexOptions.Compiled);
    private static readonly Regex Protected = new(@"<[^>]*>|\{[^}]*\}|\[[^\]]*\]|\$[^$\r\n]+\$|%[-+\d.$]*[a-zA-Z]|LocKey#\d+", RegexOptions.Compiled);
    public static string ValidateName(string value)
    {
        var name = value.Trim().Normalize(NormalizationForm.FormC);
        if (name.Length is < 1 or > 64 || !name.Any(char.IsLetterOrDigit)) throw new ArgumentException("Enter a name of 1–64 characters containing a letter or number.");
        if (name.Any(c => char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format || "<>[]{}%$\\".Contains(c)))
            throw new ArgumentException("Names cannot contain control characters or localization markup: < > [ ] { } % $ \\" );
        return name;
    }
    public static string Replace(string original, string name, bool includeFullNames = true)
        => ReplaceCharacter(original, name, "v", includeFullNames);
    public static string ReplaceCharacter(string original, string name, string characterId, bool includeFullNames = true)
        => ReplaceMany(original, [(characterId, name, includeFullNames)]);

    // Resolve all matches against the original before inserting any replacement.
    // Longest aliases win, so full names become one name and swaps cannot cascade.
    public static string ReplaceMany(string original, IEnumerable<(string CharacterId, string Name, bool FullNames)> rules)
    {
        var protectedSpans = Protected.Matches(original).Cast<Match>().ToArray();
        var allCaps = original.Count(char.IsLetter) > 1 && original == original.ToUpperInvariant();
        // A literal game newline (two characters, backslash+n) is a boundary too.
        var boundaryText = EscapedWhitespace.Replace(original, "  ");
        var matches = rules.SelectMany(rule =>
        {
            var name = ValidateName(rule.Name);
            return Patterns[rule.CharacterId].Matches(boundaryText).Cast<Match>()
                .Where(m => rule.CharacterId != "v" || rule.FullNames || m.Value == "V")
                .Select(m => (Match: m, Name: name, rule.CharacterId));
        }).OrderBy(x => x.Match.Index).ThenByDescending(x => x.Match.Length);
        var result = new StringBuilder(); int offset = 0;
        foreach (var hit in matches)
        {
            var m = hit.Match; var name = hit.Name;
            if (m.Index < offset) continue;
            if (protectedSpans.Any(p => m.Index < p.Index + p.Length && m.Index + m.Length > p.Index)) continue;
            var end = m.Index + m.Length;
            if (m.Value == "V" && (
                (m.Index >= 2 && ".-".Contains(boundaryText[m.Index - 1]) && char.IsLetterOrDigit(boundaryText[m.Index - 2])) ||
                (end + 1 < boundaryText.Length && boundaryText[end] == '-' && char.IsLetterOrDigit(boundaryText[end + 1])) ||
                (end + 1 < boundaryText.Length && boundaryText[end] == '.' && char.IsDigit(boundaryText[end + 1])))) continue;
            result.Append(original, offset, m.Index - offset);
            result.Append(allCaps || (m.Value.Length > 1 && m.Value == m.Value.ToUpperInvariant()) ? name.ToUpperInvariant() : name);
            offset = end;
        }
        result.Append(original, offset, original.Length - offset);
        return result.ToString();
    }
    public static bool IsCandidate(string original) => Replace(original, "__Candidate__") != original;
    public static bool HasStandaloneV(string original) => Replace(original, "__Candidate__", false) != original;
    public static bool IsCandidate(string original, string characterId) => ReplaceCharacter(original, "__Candidate__", characterId) != original;
}
