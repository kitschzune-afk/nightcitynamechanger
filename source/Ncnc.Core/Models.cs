using System.Text.Json;
using System.Security.Cryptography;
namespace Ncnc;

public enum Category { Dialogue, Journal, Messages, UI, Unclassified }
public sealed class Candidate
{
    public bool Include { get; set; }
    public bool ReplaceFullNames { get; set; }
    public string Resource { get; set; } = "";
    public string Id { get; set; } = "";
    public string SecondaryKey { get; set; } = "";
    public string Field { get; set; } = "";
    public string Original { get; set; } = "";
    public string Replacement { get; set; } = "";
    public Category Category { get; set; }
    public string Evidence { get; set; } = "";
    public string Review { get; set; } = "Needs review";
    public string Identity => Resource + "|" + Id + "|" + Field;
}
public sealed record Reviewed(string Resource, string Id, string Field, string OriginalHash, Category Category, string Note);
public sealed record IndexData(string Fingerprint, string Root, List<Candidate> Entries, int Resources, int Strings);
public sealed record BuildResult(string Archive, string Sha256, string Fingerprint, string Game, List<string> Resources, int Replacements);
public sealed record InstallManifest(string Owner, string Game, string FileName, string Sha256, DateTime InstalledUtc);
public sealed class Settings
{
    public string Name { get; set; } = "Johnny";
    public string Game { get; set; } = "";
    public string Tool { get; set; } = "";
    public List<Category> Categories { get; set; } = [Category.Dialogue, Category.Journal, Category.Messages, Category.UI];
    public List<string> Excluded { get; set; } = [];
    public List<Reviewed> Approvals { get; set; } = [];
}
public static class Storage
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, MaxDepth = 256 };
    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
    public static string FileHash(string path) { using var s = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(s)); }
    public static T Read<T>(string p) => JsonSerializer.Deserialize<T>(File.ReadAllText(p), Json) ?? throw new InvalidDataException(p);
    public static void Write<T>(string p, T data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(p))!);
        var temp = p + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Json));
        File.Move(temp, p, true);
    }
    public static string Child(string root, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (Path.IsPathRooted(relative) || !full.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsafe relative path: " + relative);
        return full;
    }
}
