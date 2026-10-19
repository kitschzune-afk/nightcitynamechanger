using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Ncnc;
public static class Replacement
{
    // Word boundaries protect words/identifiers. Compound names and abbreviations are checked separately.
    private static readonly Regex Names = new(@"(?<![\p{L}\p{M}\p{N}_])(?:V|Vincent|Valerie|VINCENT|VALERIE)(?![\p{L}\p{M}\p{N}_])", RegexOptions.Compiled);
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
    {
        name = ValidateName(name);
        var protectedSpans = Protected.Matches(original).Cast<Match>().ToArray();
        var allCaps = original.Count(char.IsLetter) > 1 && original == original.ToUpperInvariant();
        // A literal game newline (two characters, backslash+n) is a boundary too.
        var boundaryText = EscapedWhitespace.Replace(original, "  ");
        var result = new StringBuilder(); int offset = 0;
        foreach (Match m in Names.Matches(boundaryText))
        {
            if (!includeFullNames && m.Value != "V") continue;
            if (protectedSpans.Any(p => m.Index >= p.Index && m.Index < p.Index + p.Length)) continue;
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
}
